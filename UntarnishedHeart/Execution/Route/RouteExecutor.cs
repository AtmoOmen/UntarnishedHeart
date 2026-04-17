using System.Numerics;
using Dalamud.Game.ClientState.Conditions;
using FFXIVClientStructs.FFXIV.Client.UI;
using OmenTools.Dalamud;
using OmenTools.Info.Game.Enums;
using OmenTools.Interop.Game;
using OmenTools.Interop.Windows.Helpers;
using OmenTools.OmenService;
using UntarnishedHeart.Execution.Common;
using UntarnishedHeart.Execution.Condition;
using UntarnishedHeart.Execution.Enums;
using UntarnishedHeart.Execution.ExecuteAction;
using UntarnishedHeart.Execution.ExecuteAction.Implementations;
using UntarnishedHeart.Execution.Preset;
using UntarnishedHeart.Execution.Preset.Enums;
using UntarnishedHeart.Execution.Preset.Helpers;
using UntarnishedHeart.Execution.Route.Enums;
using UntarnishedHeart.Internal;

namespace UntarnishedHeart.Execution.Route;

public sealed class RouteExecutor
(
    Route                     route,
    ExecuteActionRuntimeCursor? startCursor = null
) : ExecuteActionExecutionHost, IDisposable
{
    private CancellationTokenSource? cancelToken;
    private Task?                    executionTask;
    private CancellationTokenSource? movementCancellationSource;
    private Task?                    movementTask;
    private string                   currentPresetName   = string.Empty;
    private string                   routeRunningMessage = string.Empty;
    private readonly ExecuteActionRuntimeCursor? initialStartCursor = startCursor == null ? null : new(startCursor.StepIndex, startCursor.Phase, startCursor.ActionIndex);

    public Route SourceRoute { get; } = route;

    public List<PresetStep> Steps { get; } = route.Steps;

    public int CurrentStepIndex { get; private set; }

    public PresetExecutor? CurrentExecutor { get; private set; }

    public RouteExecutorState State { get; private set; } = RouteExecutorState.NotStarted;

    public bool IsRunning => State is RouteExecutorState.Running or RouteExecutorState.WaitingForExecutor;

    public bool IsFinished => State == RouteExecutorState.Completed;

    public bool IsDisposed { get; private set; }

    public bool IsStopAfterDutyCompletionRequested { get; private set; }

    public string RunningMessage
    {
        get
        {
            if (CurrentExecutor is { Completion.IsCompleted: false })
                return $"步骤 {CurrentStepIndex}: {GetCurrentStepName()} - {CurrentExecutor.Progress.RunningMessage}";

            if (!string.IsNullOrWhiteSpace(routeRunningMessage))
                return routeRunningMessage;

            return State switch
            {
                RouteExecutorState.NotStarted => "路线未运行",
                RouteExecutorState.Completed  => "路线已完成",
                RouteExecutorState.Stopped    => "路线已停止",
                RouteExecutorState.Error      => "路线执行出错",
                _                             => $"步骤 {CurrentStepIndex}: {GetCurrentStepName()}"
            };
        }
    }

    public RouteExecutionCursor ExecutionCursor =>
        new()
        {
            RouteCursor = CurrentRuntimeCursor.HasStep
                              ? CurrentRuntimeCursor
                              : new(CurrentStepIndex, null, -1),
            PresetCursor = CurrentExecutor is { Completion.IsCompleted: false } currentExecutor ? currentExecutor.Progress.RuntimeCursor : null
        };

    private int CompletedDutyCount { get; set; }

    public void Dispose()
    {
        if (IsDisposed) return;

        Stop();

        try
        {
            executionTask?.Wait(TimeSpan.FromSeconds(5));
        }
        catch (AggregateException)
        {
        }

        movementCancellationSource?.Dispose();
        movementCancellationSource = null;
        movementTask               = null;

        cancelToken?.Dispose();
        cancelToken   = null;
        executionTask = null;

        DisposeCurrentExecutor();
        IsDisposed = true;
    }

    public async Task StartAsync()
    {
        if (IsRunning || Steps.Count == 0 || IsDisposed) return;

        ResetRouteProgress();
        ResetRuntimeCursor();
        State                              = RouteExecutorState.Running;
        IsStopAfterDutyCompletionRequested = false;
        routeRunningMessage                = string.Empty;

        cancelToken?.Dispose();
        cancelToken = new CancellationTokenSource();

        try
        {
            executionTask = ExecuteRouteAsync(cancelToken.Token);
            await executionTask;
        }
        catch (OperationCanceledException)
        {
            State = RouteExecutorState.Stopped;
        }
        catch (Exception ex)
        {
            State = RouteExecutorState.Error;
            NotifyHelper.Instance().Chat($"路线执行出错: {ex.Message}");
        }
    }

    public void Start() =>
        _ = DService.Instance().Framework.Run(StartAsync);

    public void Stop()
    {
        if (State is RouteExecutorState.NotStarted or RouteExecutorState.Completed or RouteExecutorState.Stopped)
            return;

        IsStopAfterDutyCompletionRequested = false;
        cancelToken?.Cancel();
        State = RouteExecutorState.Stopped;
        ResetRuntimeCursor();

        CancelMovement();
        DisposeCurrentExecutor();
    }

    public bool RequestStopAfterDutyCompletion()
    {
        if (!IsRunning)
            return false;

        IsStopAfterDutyCompletionRequested = true;
        CurrentExecutor?.RequestStopAfterDutyCompletion();
        return true;
    }

    public bool CancelStopAfterDutyCompletionRequest()
    {
        if (!IsStopAfterDutyCompletionRequested)
            return false;

        IsStopAfterDutyCompletionRequested = false;
        CurrentExecutor?.CancelStopAfterDutyCompletionRequest();
        return true;
    }

    private async Task ExecuteRouteAsync(CancellationToken cancellationToken)
    {
        var nextStartCursor = initialStartCursor;

        while (CurrentStepIndex < Steps.Count             &&
               !cancellationToken.IsCancellationRequested &&
               State is RouteExecutorState.Running or RouteExecutorState.WaitingForExecutor)
        {
            var step       = Steps[CurrentStepIndex];
            var stepResult = await ExecuteStepAsync
            (
                step,
                CurrentStepIndex,
                nextStartCursor is { StepIndex: var startStepIndex } && startStepIndex == CurrentStepIndex ? nextStartCursor : null,
                cancellationToken
            );
            nextStartCursor = null;

            switch (stepResult.Kind)
            {
                case ActionFlowKind.Continue:
                    CurrentStepIndex++;
                    break;
                case ActionFlowKind.JumpToStep:
                    CurrentStepIndex = stepResult.Index;
                    break;
                case ActionFlowKind.RestartCurrentStep:
                    break;
                case ActionFlowKind.LeaveAndEnd:
                    State = RouteExecutorState.Completed;
                    NotifyHelper.Instance().Chat("路线执行完成");
                    return;
                case ActionFlowKind.LeaveAndRestart:
                    ResetRouteProgress();
                    State = RouteExecutorState.Running;
                    nextStartCursor = initialStartCursor;
                    break;
                default:
                    throw new InvalidOperationException($"不支持的步骤跳转结果: {stepResult.Kind}");
            }
        }

        if (CurrentStepIndex >= Steps.Count && State == RouteExecutorState.Running)
        {
            State = RouteExecutorState.Completed;
            NotifyHelper.Instance().Chat("路线执行完成");
        }
    }

    protected override async Task<ActionFlowResult?> ExecuteCustomActionCoreAsync
    (
        int               stepIndex,
        PresetStep        step,
        PresetStepPhase   phase,
        int               actionIndex,
        ExecuteActionBase action,
        int               currentPhaseActionCount,
        string            actionLabel,
        CancellationToken cancellationToken
    )
    {
        if (action is not ExecutePresetAction executePresetAction)
            return null;

        return await ExecutePresetActionAsync(actionLabel, executePresetAction, cancellationToken);
    }

    private async Task<ActionFlowResult> ExecutePresetActionAsync
    (
        string              actionLabel,
        ExecutePresetAction action,
        CancellationToken   cancellationToken
    )
    {
        if (string.IsNullOrWhiteSpace(action.PresetName))
            throw new InvalidOperationException("执行预设动作缺少目标预设名称");

        var preset = PluginConfig.Instance().Presets.FirstOrDefault(candidate => string.Equals(candidate.Name, action.PresetName, StringComparison.Ordinal));
        if (preset is not { IsValid: true })
            throw new InvalidOperationException($"无法找到有效预设: {action.PresetName}");

        if (!string.Equals(currentPresetName, action.PresetName, StringComparison.Ordinal))
        {
            DLog.Debug("路线执行预设发生变化，重置副本计数");
            CompletedDutyCount = 0;
            currentPresetName  = action.PresetName;
        }

        DisposeCurrentExecutor();

        SetRunningMessage($"{actionLabel} - 开始执行预设: {preset.Name}");
        CurrentExecutor = new PresetExecutor(preset, action.DutyOptions.ToRunOptions());
        if (IsStopAfterDutyCompletionRequested)
            CurrentExecutor.RequestStopAfterDutyCompletion();

        CurrentExecutor.Start();
        State = RouteExecutorState.WaitingForExecutor;

        var result = await CurrentExecutor.Completion.WaitAsync(cancellationToken);
        DisposeCurrentExecutor();

        switch (result.EndReason)
        {
            case ExecutorEndReason.Error:
                throw new InvalidOperationException($"预设执行出错: {result.ErrorMessage}");
            case ExecutorEndReason.InvalidPreset:
                throw new InvalidOperationException($"预设无效: {action.PresetName}");
            case ExecutorEndReason.Stopped:
                State = RouteExecutorState.Stopped;
                return ActionFlowResult.Continue();
            case ExecutorEndReason.CompletedAfterDuty:
                State = RouteExecutorState.Stopped;
                NotifyHelper.Instance().Chat("已在副本完成并退出后停止路线执行");
                return ActionFlowResult.Continue();
        }

        await WaitForAreaReadyAsync(cancellationToken);

        CompletedDutyCount += (int)result.CompletedRounds;
        State              =  RouteExecutorState.Running;
        return ActionFlowResult.Continue();
    }

    protected override ConditionContext CreateConditionContext() => ConditionContext.Create(CompletedDutyCount);

    protected override void SetRunningMessage(string message) => routeRunningMessage = message;

    protected override void ValidateStepIndex(int stepIndex)
    {
        if (stepIndex < 0 || stepIndex >= Steps.Count)
            throw new InvalidOperationException($"无效的步骤索引: {stepIndex}");
    }

    protected override void ValidateActionIndex(int actionIndex, int actionCount)
    {
        if (actionIndex < 0 || actionIndex >= actionCount)
            throw new InvalidOperationException($"无效的执行动作索引: {actionIndex}");
    }

    protected override void LeaveDuty() =>
        ExecuteCommandManager.Instance().ExecuteCommand(ExecuteCommandFlag.LeaveDuty, DService.Instance().Condition[ConditionFlag.InCombat] ? 1U : 0);

    protected override async Task LeaveDutyAndRestartAsync(string message, CancellationToken cancellationToken)
    {
        SetRunningMessage(message);
        LeaveDuty();
        await WaitForDutyExitAsync(cancellationToken);
    }

    protected override async Task RunCommandsAsync(string commands, string actionLabel, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(commands))
            return;

        foreach (var command in commands.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (command.StartsWith("/wait", StringComparison.OrdinalIgnoreCase))
            {
                var split = command.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

                if (split.Length == 2 && int.TryParse(split[1], out var waitTime))
                {
                    await DelayAsync(waitTime, $"{actionLabel} - 特殊文本等待", cancellationToken);
                    continue;
                }
            }

            SetRunningMessage($"{actionLabel} - {command}");
            ChatManager.Instance().SendCommand(command);
            await Task.Delay(100, cancellationToken);
        }
    }

    protected override async Task ExecuteNearestInteractAsync(string sourceName, CancellationToken cancellationToken)
    {
        var target = PresetTargetResolver.FindNearestInteractableObject();

        if (target == null)
        {
            SetRunningMessage($"未找到可交互物体: {sourceName}");
            return;
        }

        await WaitUntilAsync
        (
            () => !DService.Instance().Condition.IsOnMount         &&
                  !DService.Instance().Condition.IsOccupiedInEvent &&
                  UIModule.IsScreenReady()                         &&
                  target.TargetInteract(),
            $"交互最近可交互物体: {sourceName}",
            cancellationToken
        );

        PresetTargetResolver.OpenObjectInteraction(target);
    }

    protected override async Task ExecuteMovementActionAsync(MoveToPositionAction action, string actionLabel, CancellationToken cancellationToken)
    {
        if (action.Position == default)
            return;

        switch (action.MoveType)
        {
            case MoveType.简单移动:
                SetRunningMessage(actionLabel);
                StartPathfindMovement(action.Position, cancellationToken);
                break;
            case MoveType.寻路:
                SetRunningMessage(actionLabel);
                StartVnavmeshMovement(action.Position, cancellationToken);
                break;
            case MoveType.无:
            case MoveType.传送:
            default:
                SetRunningMessage(actionLabel);
                Teleport(action.Position);
                break;
        }

        await Task.CompletedTask;
    }

    protected override async Task WaitUntilAsync(Func<bool> predicate, string message, CancellationToken cancellationToken, int intervalMs = 100)
    {
        SetRunningMessage(message);
        while (!predicate())
            await Task.Delay(intervalMs, cancellationToken);
    }

    protected override async Task DelayAsync(int delayMs, string message, CancellationToken cancellationToken)
    {
        SetRunningMessage(message);
        await Task.Delay(delayMs, cancellationToken);
    }

    private async Task WaitForAreaReadyAsync(CancellationToken cancellationToken)
    {
        await WaitUntilAsync
        (
            () =>
            {
                var condition = DService.Instance().Condition;
                return !condition.IsBoundByDuty && !condition.IsBetweenAreas && UIModule.IsScreenReady();
            },
            "等待区域加载结束",
            cancellationToken
        );
    }

    private async Task WaitForDutyExitAsync(CancellationToken cancellationToken)
    {
        await WaitUntilAsync
        (
            () =>
            {
                var condition = DService.Instance().Condition;
                return !DService.Instance().DutyState.IsDutyStarted &&
                       !condition.IsBoundByDuty                     &&
                       !condition.IsBetweenAreas                    &&
                       UIModule.IsScreenReady();
            },
            "等待退出副本",
            cancellationToken
        );
    }

    private void ResetRouteProgress()
    {
        CompletedDutyCount  = 0;
        CurrentStepIndex    = initialStartCursor?.StepIndex ?? 0;
        currentPresetName   = string.Empty;
        routeRunningMessage = string.Empty;
        ResetRuntimeCursor();
    }

    private string GetCurrentStepName() =>
        CurrentStepIndex >= 0 && CurrentStepIndex < Steps.Count ? Steps[CurrentStepIndex].Name : "未知步骤";

    private void DisposeCurrentExecutor()
    {
        CurrentExecutor?.Dispose();
        CurrentExecutor = null;
    }

    private static unsafe void Teleport(Vector3 position)
    {
        if (DService.Instance().ObjectTable.LocalPlayer is not { } localPlayer)
            return;

        localPlayer.ToStruct()->SetPosition(position.X, position.Y, position.Z);
        KeyEmulationHelper.SendKeypress(Keys.W);
    }

    private void StartPathfindMovement(Vector3 position, CancellationToken parentToken) =>
        StartMovement
        (
            async token =>
            {
                using var movementController = new MovementInputController();
                movementController.DesiredPosition = position;
                movementController.Enabled         = true;

                try
                {
                    while (!token.IsCancellationRequested)
                    {
                        if (DService.Instance().ObjectTable.LocalPlayer is not { } localPlayer)
                        {
                            await Task.Delay(100, token);
                            continue;
                        }

                        if (Vector3.DistanceSquared(localPlayer.Position, position) <= 2f)
                            break;

                        await Task.Delay(500, token);
                    }
                }
                finally
                {
                    movementController.Enabled         = false;
                    movementController.DesiredPosition = default;
                }
            },
            parentToken
        );

    private void StartVnavmeshMovement(Vector3 position, CancellationToken parentToken) =>
        StartMovement(token => RunVnavmeshMovementAsync(position, false, token), parentToken);

    private void StartMovement(Func<CancellationToken, Task> workFactory, CancellationToken parentToken)
    {
        CancelMovement();

        var movementCts = CancellationTokenSource.CreateLinkedTokenSource(parentToken);
        movementCancellationSource = movementCts;

        movementTask = DService.Instance().Framework.Run
        (
            async () =>
            {
                try
                {
                    await workFactory(movementCts.Token);
                }
                catch (OperationCanceledException) when (movementCts.IsCancellationRequested)
                {
                }
                catch (Exception ex)
                {
                    NotifyHelper.Instance().Chat($"移动执行失败: {ex.Message}");
                }
                finally
                {
                    if (ReferenceEquals(movementCancellationSource, movementCts))
                    {
                        movementCancellationSource = null;
                        movementTask               = null;
                    }

                    movementCts.Dispose();
                }
            },
            movementCts.Token
        );
    }

    private async Task RunVnavmeshMovementAsync(Vector3 position, bool fly, CancellationToken cancellationToken)
    {
        try
        {
            var timeout = DateTime.Now.AddSeconds(10);
            while (!vnavmeshIPC.GetIsNavReady() && DateTime.Now < timeout)
                await Task.Delay(100, cancellationToken);

            if (!vnavmeshIPC.GetIsNavReady())
            {
                NotifyHelper.Instance().ChatError("vnavmesh 未准备就绪");
                return;
            }

            if (!vnavmeshIPC.PathfindAndMoveTo(position, fly))
            {
                NotifyHelper.Instance().ChatError("vnavmesh 寻路启动失败");
                return;
            }

            await Task.Delay(500, cancellationToken);

            while (!cancellationToken.IsCancellationRequested)
            {
                if (DService.Instance().ObjectTable.LocalPlayer is not { } localPlayer)
                {
                    await Task.Delay(100, cancellationToken);
                    continue;
                }

                var distance = Vector3.Distance(localPlayer.Position, position);
                if (distance <= 2f)
                    break;

                if (!vnavmeshIPC.GetIsPathfindRunning() && !vnavmeshIPC.GetIsNavPathfindInProgress())
                {
                    await Task.Delay(500, cancellationToken);
                    distance = Vector3.Distance(localPlayer.Position, position);

                    if (distance > 2f)
                        NotifyHelper.Instance().Chat($"vnavmesh 寻路结束但未到达目标，距离: {distance:F2} 米");

                    break;
                }

                await Task.Delay(100, cancellationToken);
            }
        }
        finally
        {
            vnavmeshIPC.StopPathfind();
        }
    }

    private void CancelMovement()
    {
        if (movementCancellationSource is not { IsCancellationRequested: false } movementCts)
            return;

        movementCts.Cancel();
    }
}
