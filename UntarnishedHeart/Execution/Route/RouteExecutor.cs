using Dalamud.Game.ClientState.Conditions;
using FFXIVClientStructs.FFXIV.Client.UI;
using OmenTools.Dalamud;
using OmenTools.Info.Game.Enums;
using OmenTools.OmenService;
using UntarnishedHeart.Execution.Common;
using UntarnishedHeart.Execution.Condition;
using UntarnishedHeart.Execution.Enums;
using UntarnishedHeart.Execution.ExecuteAction;
using UntarnishedHeart.Execution.ExecuteAction.Implementations;
using UntarnishedHeart.Execution.Preset;
using UntarnishedHeart.Execution.Route.Enums;
using UntarnishedHeart.Internal;

namespace UntarnishedHeart.Execution.Route;

public sealed class RouteExecutor
(
    Route                       route,
    ExecuteActionRuntimeCursor? startCursor = null
) : ExecuteActionExecutionHost, IDisposable
{
    private CancellationTokenSource? cancelToken;
    private Task?                    executionTask;
    private string                   currentPresetName   = string.Empty;
    private string                   routeRunningMessage = string.Empty;

    private readonly ExecuteActionRuntimeCursor? initialStartCursor = startCursor == null ?
                                                                          null :
                                                                          new(startCursor.StepIndex, startCursor.ActionIndex);

    private ExecuteActionRuntimeCursor? pendingNavigation;
    private int                         executionVersion;

    public Route SourceRoute { get; } = route;

    public List<PresetStep> Steps { get; } = route.Steps;

    public int CurrentStepIndex { get; private set; }

    public PresetExecutor? CurrentExecutor { get; private set; }

    public RouteExecutorState State { get; private set; } = RouteExecutorState.NotStarted;

    public bool IsRunning => State is RouteExecutorState.Running or RouteExecutorState.WaitingForExecutor;

    internal bool CanNavigate => IsRunning;

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
            RouteCursor = CurrentRuntimeCursor.HasStep ?
                              CurrentRuntimeCursor :
                              new(CurrentStepIndex, -1),
            PresetCursor = CurrentExecutor is { Completion.IsCompleted: false } currentExecutor ?
                               currentExecutor.Progress.RuntimeCursor :
                               null
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

        Movement.Dispose();

        cancelToken?.Dispose();
        cancelToken   = null;
        executionTask = null;

        DisposeCurrentExecutor();
        IsDisposed = true;
    }

    public void Start()
    {
        if (IsRunning || Steps.Count == 0 || IsDisposed) return;

        ResetRouteProgress();
        ResetRuntimeCursor();
        State                              = RouteExecutorState.Running;
        IsStopAfterDutyCompletionRequested = false;
        routeRunningMessage                = string.Empty;

        _ = DService.Instance().Framework.Run(StartAsync);
    }

    public async Task StartAsync()
    {
        if (State != RouteExecutorState.Running) return;

        cancelToken?.Dispose();
        var currentCancelToken = new CancellationTokenSource();
        cancelToken = currentCancelToken;

        var versionAtStart = executionVersion;

        try
        {
            executionTask = ExecuteRouteAsync(currentCancelToken.Token);
            await executionTask;
        }
        catch (OperationCanceledException)
        {
            if (versionAtStart == executionVersion)
                State = RouteExecutorState.Stopped;
        }
        catch (Exception ex)
        {
            if (versionAtStart == executionVersion)
            {
                State = RouteExecutorState.Error;
                NotifyHelper.Instance().Chat($"路线执行出错: {ex.Message}");
            }
        }
    }

    public void Stop()
    {
        if (State is RouteExecutorState.NotStarted or RouteExecutorState.Completed or RouteExecutorState.Stopped)
            return;

        IsStopAfterDutyCompletionRequested = false;
        cancelToken?.Cancel();
        State = RouteExecutorState.Stopped;
        ResetRuntimeCursor();

        Movement.Cancel();
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

    public void NavigateTo
    (
        int stepIndex,
        int actionIndex = -1
    )
    {
        if (!IsRunning)
            return;

        if (!IsValidNavigation(stepIndex, actionIndex))
            return;

        executionVersion++;
        pendingNavigation   = new ExecuteActionRuntimeCursor(stepIndex, actionIndex);
        CurrentStepIndex    = stepIndex;
        routeRunningMessage = string.Empty;
        cancelToken?.Cancel();
        Movement.Cancel();
        DisposeCurrentExecutor();
        ResetRuntimeCursor();

        State = RouteExecutorState.Running;
        _     = DService.Instance().Framework.Run(StartAsync);
    }

    private bool IsValidNavigation
    (
        int stepIndex,
        int actionIndex
    )
    {
        if (stepIndex < 0 || stepIndex >= Steps.Count)
            return false;

        if (actionIndex < 0)
            return true;

        return actionIndex < Steps[stepIndex].Actions.Count;
    }

    private async Task ExecuteRouteAsync
    (
        CancellationToken cancellationToken
    )
    {
        var nextStartCursor = pendingNavigation ?? initialStartCursor;
        pendingNavigation = null;

        while (CurrentStepIndex < Steps.Count             &&
               !cancellationToken.IsCancellationRequested &&
               State is RouteExecutorState.Running or RouteExecutorState.WaitingForExecutor)
        {
            var step = Steps[CurrentStepIndex];
            var stepResult = await ExecuteStepAsync
                             (
                                 step,
                                 CurrentStepIndex,
                                 nextStartCursor is { StepIndex: var startStepIndex } && startStepIndex == CurrentStepIndex ?
                                     nextStartCursor :
                                     null,
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
                case ActionFlowKind.LeaveAndEnd:
                    State = RouteExecutorState.Completed;
                    NotifyHelper.Instance().Chat("路线执行完成");
                    return;
                case ActionFlowKind.LeaveAndRestart:
                    ResetRouteProgress();
                    State           = RouteExecutorState.Running;
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
        int               actionIndex,
        ExecuteActionBase action,
        int               currentActionCount,
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
        if (string.IsNullOrWhiteSpace(action.PresetID))
            throw new InvalidOperationException($"预设引用未绑定: {action.PresetName}");

        var preset = PluginConfig.Instance().Presets.FirstOrDefault(candidate => string.Equals(candidate.ID, action.PresetID, StringComparison.Ordinal));
        if (preset is not { IsValid: true })
            throw new InvalidOperationException($"无法找到有效预设: {action.PresetName}");

        if (!string.Equals(currentPresetName, preset.Name, StringComparison.Ordinal))
        {
            DLog.Debug("路线执行预设发生变化，重置副本计数");
            CompletedDutyCount = 0;
            currentPresetName  = preset.Name;
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
                if (!cancellationToken.IsCancellationRequested)
                    State = RouteExecutorState.Stopped;
                return ActionFlowResult.Continue();
            case ExecutorEndReason.CompletedAfterDuty:
                if (!cancellationToken.IsCancellationRequested)
                {
                    State = RouteExecutorState.Stopped;
                    NotifyHelper.Instance().Chat("已在副本完成并退出后停止路线执行");
                }

                return ActionFlowResult.Continue();
        }

        await WaitForAreaReadyAsync(cancellationToken);

        CompletedDutyCount += (int)result.CompletedRounds;
        State              =  RouteExecutorState.Running;
        return ActionFlowResult.Continue();
    }

    protected override ConditionContext CreateConditionContext() => ConditionContext.Create(CompletedDutyCount);

    protected override void SetRunningMessage
    (
        string message
    ) => routeRunningMessage = message;

    protected override void ValidateStepIndex
    (
        int stepIndex
    )
    {
        if (stepIndex < 0 || stepIndex >= Steps.Count)
            throw new InvalidOperationException($"无效的步骤索引: {stepIndex}");
    }

    protected override void ValidateActionIndex
    (
        int actionIndex,
        int actionCount
    )
    {
        if (actionIndex < 0 || actionIndex >= actionCount)
            throw new InvalidOperationException($"无效的执行动作索引: {actionIndex}");
    }

    protected override void LeaveDuty() =>
        ExecuteCommandManager.Instance().ExecuteCommand
        (
            ExecuteCommandFlag.LeaveDuty,
            DService.Instance().Condition[ConditionFlag.InCombat] ?
                1U :
                0
        );

    protected override async Task LeaveDutyAndRestartAsync
    (
        string            message,
        CancellationToken cancellationToken
    )
    {
        SetRunningMessage(message);
        LeaveDuty();
        await WaitForDutyExitAsync(cancellationToken);
    }

    private async Task WaitForAreaReadyAsync
    (
        CancellationToken cancellationToken
    ) =>
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

    private async Task WaitForDutyExitAsync
    (
        CancellationToken cancellationToken
    ) =>
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

    private void ResetRouteProgress()
    {
        CompletedDutyCount  = 0;
        CurrentStepIndex    = initialStartCursor?.StepIndex ?? 0;
        currentPresetName   = string.Empty;
        routeRunningMessage = string.Empty;
        ResetRuntimeCursor();
    }

    private string GetCurrentStepName() =>
        CurrentStepIndex >= 0 && CurrentStepIndex < Steps.Count ?
            Steps[CurrentStepIndex].Name :
            "未知步骤";

    private void DisposeCurrentExecutor()
    {
        CurrentExecutor?.Dispose();
        CurrentExecutor = null;
    }
}
