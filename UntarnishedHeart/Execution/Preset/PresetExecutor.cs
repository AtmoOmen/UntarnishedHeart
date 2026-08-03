using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.DutyState;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using Lumina.Excel.Sheets;
using OmenTools.Info.Game.Enums;
using OmenTools.Interop.Game.Helpers;
using OmenTools.Interop.Game.Lumina;
using OmenTools.OmenService;
using OmenTools.Threading;
using UntarnishedHeart.Execution.Common;
using UntarnishedHeart.Execution.Condition;
using UntarnishedHeart.Execution.Enums;

namespace UntarnishedHeart.Execution.Preset;

public class PresetExecutor : ExecuteActionExecutionHost, IDisposable
{
    private readonly SemaphoreSlim                              manualInteractGate = new(1, 1);
    private readonly TaskCompletionSource<PresetExecutorResult> completionSource   = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly PresetExecutorRunOptions                   runOptions;

    private          CancellationTokenSource?    executorCancellationSource;
    private          CancellationTokenSource?    currentWorkCancellationSource;
    private          Task?                       currentWorkTask;
    private          bool                        isStarted;
    private          bool                        listenersRegistered;
    private volatile ExecuteActionRuntimeCursor? pendingNavigation;
    private volatile bool                        isPresetRunActive;

    internal PresetExecutor
    (
        Preset?                  preset,
        PresetExecutorRunOptions runOptions
    )
    {
        ExecutorPreset  = preset;
        this.runOptions = runOptions;
    }

    public uint CurrentRound { get; private set; }

    public int MaxRound => runOptions.MaxRound;

    public Preset? ExecutorPreset { get; }

    public string RunningMessage { get; private set; } = string.Empty;

    public bool IsDisposed { get; private set; }

    public bool IsFinished => Result?.EndReason is ExecutorEndReason.Completed or ExecutorEndReason.CompletedAfterDuty;

    public bool IsStopped => Result?.EndReason == ExecutorEndReason.Stopped;

    public bool IsStopAfterDutyCompletionRequested { get; private set; }

    internal bool CanNavigate => isPresetRunActive;

    internal Task<PresetExecutorResult> Completion => completionSource.Task;

    internal PresetExecutorResult? Result { get; private set; }

    internal PresetExecutorProgress Progress =>
        new()
        {
            CurrentRound   = CurrentRound,
            MaxRound       = MaxRound,
            RunningMessage = RunningMessage,
            IsFinished     = IsFinished,
            IsStopped      = IsStopped,
            RuntimeCursor  = CurrentRuntimeCursor
        };

    public void Start()
    {
        if (IsDisposed || isStarted || Completion.IsCompleted)
            return;

        isStarted = true;
        ResetRuntimeCursor();

        if (ExecutorPreset is not { IsValid: true })
        {
            Finish
            (
                new PresetExecutorResult
                {
                    EndReason       = ExecutorEndReason.InvalidPreset,
                    CompletedRounds = CurrentRound
                },
                true
            );
            return;
        }

        executorCancellationSource = new CancellationTokenSource();
        RegisterListeners();

        if (DService.Instance().ClientState.TerritoryType == ExecutorPreset.Zone)
            OnDutyStarted(null);
        else if (!DService.Instance().Condition.IsOccupiedInEvent && runOptions.LeaderMode)
            ReplaceCurrentWork(RegisterDutyAsync);
    }

    public void Stop()
    {
        if (Completion.IsCompleted)
            return;

        executorCancellationSource?.Cancel();
        AbortPrevious();
        Finish
        (
            new PresetExecutorResult
            {
                EndReason       = ExecutorEndReason.Stopped,
                CompletedRounds = CurrentRound
            },
            false
        );
    }

    public void Dispose()
    {
        if (IsDisposed) return;

        Stop();

        currentWorkCancellationSource?.Dispose();
        currentWorkCancellationSource = null;

        Movement.Dispose();

        executorCancellationSource?.Dispose();
        executorCancellationSource = null;

        manualInteractGate.Dispose();
        UnregisterListeners();

        IsDisposed = true;
    }

    public void ManualEnqueueNewRound()
    {
        if (Completion.IsCompleted || ExecutorPreset is not { IsValid: true })
            return;

        ReplaceCurrentWork(token => LeaveDutyAndRestartAsync("手动退出副本开启新一局", token));
    }

    public void NavigateTo
    (
        int stepIndex,
        int actionIndex = -1
    )
    {
        if (!isPresetRunActive || Completion.IsCompleted || ExecutorPreset is not { IsValid: true })
            return;

        if (!IsValidNavigation(stepIndex, actionIndex))
            return;

        pendingNavigation = new ExecuteActionRuntimeCursor(stepIndex, actionIndex);
        ReplaceCurrentWork(RunPresetAsync);
    }

    public bool RequestNearestInteract()
    {
        if (Completion.IsCompleted || IsDisposed)
            return false;

        _ = RunManualNearestInteractAsync();
        return true;
    }

    public bool RequestStopAfterDutyCompletion()
    {
        if (Completion.IsCompleted || IsDisposed)
            return false;

        IsStopAfterDutyCompletionRequested = true;
        return true;
    }

    public bool CancelStopAfterDutyCompletionRequest()
    {
        if (Completion.IsCompleted || IsDisposed || !IsStopAfterDutyCompletionRequested)
            return false;

        IsStopAfterDutyCompletionRequested = false;
        return true;
    }

    private async Task RunManualNearestInteractAsync()
    {
        if (!await manualInteractGate.WaitAsync(0))
            return;

        try
        {
            using var commandCancellationSource = executorCancellationSource == null ?
                                                      null :
                                                      CancellationTokenSource.CreateLinkedTokenSource(executorCancellationSource.Token);

            await ExecuteNearestInteractAsync("命令触发最近交互", commandCancellationSource?.Token ?? CancellationToken.None);
        }
        catch (Exception ex)
        {
            NotifyHelper.Instance().Chat($"执行最近交互时发生错误: {ex.Message}");
        }
        finally
        {
            manualInteractGate.Release();
        }
    }

    private void RegisterListeners()
    {
        if (listenersRegistered)
            return;

        DService.Instance().AddonLifecycle.RegisterListener(AddonEvent.PostDraw, "ContentsFinderConfirm", OnAddonDraw);
        DService.Instance().ClientState.TerritoryChanged += OnZoneChanged;
        DService.Instance().DutyState.DutyStarted        += OnDutyStarted;
        DService.Instance().DutyState.DutyRecommenced    += OnDutyStarted;
        DService.Instance().DutyState.DutyCompleted      += OnDutyCompleted;

        listenersRegistered = true;
    }

    private void UnregisterListeners()
    {
        if (!listenersRegistered)
            return;

        DService.Instance().ClientState.TerritoryChanged -= OnZoneChanged;
        DService.Instance().DutyState.DutyCompleted      -= OnDutyCompleted;
        DService.Instance().DutyState.DutyStarted        -= OnDutyStarted;
        DService.Instance().DutyState.DutyRecommenced    -= OnDutyStarted;
        DService.Instance().AddonLifecycle.UnregisterListener(OnAddonDraw);

        listenersRegistered = false;
    }

    private static unsafe void OnAddonDraw
    (
        AddonEvent type,
        AddonArgs  args
    )
    {
        if (!Throttler.Shared.Throttle("自动确认进入副本节流")) return;
        if (args.Addon == nint.Zero) return;
        args.Addon.ToStruct()->Callback(8);
    }

    private void OnZoneChanged
    (
        uint zone
    )
    {
        if (ExecutorPreset == null || zone != ExecutorPreset.Zone || Completion.IsCompleted)
            return;

        AbortPrevious();
    }

    private void OnDutyStarted
    (
        IDutyStateEventArgs args
    )
    {
        if (ExecutorPreset == null || GameState.TerritoryType != ExecutorPreset.Zone || Completion.IsCompleted)
            return;

        ReplaceCurrentWork(RunPresetAsync);
    }

    private void OnDutyCompleted
    (
        IDutyStateEventArgs args
    )
    {
        if (ExecutorPreset == null || GameState.TerritoryType != ExecutorPreset.Zone || Completion.IsCompleted)
            return;

        ReplaceCurrentWork(HandleDutyCompletedAsync);
    }

    private void ReplaceCurrentWork
    (
        Func<CancellationToken, Task> workFactory
    )
    {
        AbortPrevious();

        if (Completion.IsCompleted || executorCancellationSource is not { IsCancellationRequested: false } executorCts)
            return;

        var workCancellationSource = CancellationTokenSource.CreateLinkedTokenSource(executorCts.Token);
        currentWorkCancellationSource = workCancellationSource;

        currentWorkTask = DService.Instance().Framework.Run
        (
            async () =>
            {
                try
                {
                    await workFactory(workCancellationSource.Token);
                }
                catch (OperationCanceledException) when (workCancellationSource.IsCancellationRequested)
                {
                }
                catch (Exception ex)
                {
                    Finish
                    (
                        new PresetExecutorResult
                        {
                            EndReason       = ExecutorEndReason.Error,
                            CompletedRounds = CurrentRound,
                            ErrorMessage    = ex.Message
                        },
                        false
                    );
                }
                finally
                {
                    if (ReferenceEquals(currentWorkCancellationSource, workCancellationSource))
                    {
                        currentWorkTask               = null;
                        currentWorkCancellationSource = null;
                    }

                    workCancellationSource.Dispose();
                }
            },
            workCancellationSource.Token
        );
    }

    private async Task RunPresetAsync
    (
        CancellationToken cancellationToken
    )
    {
        isPresetRunActive = true;

        try
        {
            await WaitUntilAsync(() => DService.Instance().DutyState.IsDutyStarted, "等待副本开始", cancellationToken);

            if (runOptions.AutoRecommendGear)
                await EquipRecommendedGearAsync(cancellationToken);

            var effectiveStartCursor = pendingNavigation ?? runOptions.StartCursor;
            pendingNavigation = null;
            var stepIndex       = effectiveStartCursor?.StepIndex ?? 0;
            var nextStartCursor = effectiveStartCursor;

            while (stepIndex < ExecutorPreset!.Steps.Count)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var step = ExecutorPreset.Steps[stepIndex];
                var stepResult = await ExecuteStepAsync
                                 (
                                     step,
                                     stepIndex,
                                     nextStartCursor is { StepIndex: var startStepIndex } && startStepIndex == stepIndex ?
                                         nextStartCursor :
                                         null,
                                     cancellationToken
                                 );
                nextStartCursor = null;

                switch (stepResult.Kind)
                {
                    case ActionFlowKind.Continue:
                        stepIndex++;
                        break;
                    case ActionFlowKind.JumpToStep:
                        stepIndex = stepResult.Index;
                        break;
                    case ActionFlowKind.LeaveAndEnd:
                        Finish
                        (
                            new PresetExecutorResult
                            {
                                EndReason       = ExecutorEndReason.Completed,
                                CompletedRounds = CurrentRound
                            },
                            false
                        );
                        return;
                    case ActionFlowKind.LeaveAndRestart:
                        return;
                    default:
                        throw new InvalidOperationException($"不支持的步骤跳转结果: {stepResult.Kind}");
                }
            }

            SetRunningMessage("等待副本完成");
        }
        finally
        {
            isPresetRunActive = false;
        }
    }

    private bool IsValidNavigation
    (
        int stepIndex,
        int actionIndex
    )
    {
        if (stepIndex < 0 || stepIndex >= ExecutorPreset!.Steps.Count)
            return false;

        if (actionIndex < 0)
            return true;

        return actionIndex < ExecutorPreset.Steps[stepIndex].Actions.Count;
    }

    private async Task HandleDutyCompletedAsync
    (
        CancellationToken cancellationToken
    )
    {
        if (ExecutorPreset!.AutoOpenTreasures)
            await OpenTreasuresAsync(cancellationToken);

        if (ExecutorPreset.DutyDelay > 0)
            await DelayAsync(ExecutorPreset.DutyDelay, $"等待退出延迟: {ExecutorPreset.DutyDelay} ms", cancellationToken);

        await LeaveDutyAndRestartAsync
        (
            IsStopAfterDutyCompletionRequested ?
                "副本完成, 离开副本后结束执行" :
                "副本完成, 离开副本, 进入下一局",
            cancellationToken
        );
    }

    private async Task EquipRecommendedGearAsync
    (
        CancellationToken cancellationToken
    )
    {
        SetRunningMessage("尝试切换最强装备");

        unsafe
        {
            var instance = RecommendEquipModule.Instance();
            instance->SetupForClassJob((byte)LocalPlayerState.ClassJob);
            instance->EquipRecommendedGear();
        }

        await Task.Delay(100, cancellationToken);
    }

    protected override async Task LeaveDutyAndRestartAsync
    (
        string            message,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        SetRunningMessage(message);
        LeaveDuty();
        await WaitForDutyExitAsync(cancellationToken);
        CurrentRound++;

        if (IsStopAfterDutyCompletionRequested)
        {
            Finish
            (
                new PresetExecutorResult
                {
                    EndReason       = ExecutorEndReason.CompletedAfterDuty,
                    CompletedRounds = CurrentRound
                },
                false
            );
            return;
        }

        if (HasReachedMaxRound())
        {
            Finish
            (
                new PresetExecutorResult
                {
                    EndReason       = ExecutorEndReason.Completed,
                    CompletedRounds = CurrentRound
                },
                false
            );
            return;
        }

        if (runOptions.LeaderMode)
            await RegisterDutyAsync(cancellationToken);
    }

    private bool HasReachedMaxRound() => MaxRound != -1 && CurrentRound >= MaxRound;

    private async Task WaitForDutyExitAsync
    (
        CancellationToken cancellationToken
    ) =>
        await WaitUntilAsync
        (
            () =>
            {
                if (!Throttler.Shared.Throttle("等待副本结束节流")) return false;
                return !DService.Instance().DutyState.IsDutyStarted && DService.Instance().ClientState.TerritoryType != ExecutorPreset!.Zone;
            },
            IsStopAfterDutyCompletionRequested ?
                "等待退出副本后结束" :
                "等待副本结束",
            cancellationToken
        );

    private async Task RegisterDutyAsync
    (
        CancellationToken cancellationToken
    )
    {
        await WaitForDutyExitAsync(cancellationToken);

        await WaitUntilAsync
        (
            () =>
            {
                var condition = DService.Instance().Condition;
                return DService.Instance().ObjectTable.LocalPlayer != null &&
                       !condition[ConditionFlag.BetweenAreas]              &&
                       !condition.IsBoundByDuty                            &&
                       UIModule.IsScreenReady();
            },
            "等待区域加载结束",
            cancellationToken
        );

        SetRunningMessage("等待进入下一局");

        while (!cancellationToken.IsCancellationRequested)
        {
            if (DService.Instance().Condition.Any(ConditionFlag.WaitingForDutyFinder, ConditionFlag.WaitingForDuty, ConditionFlag.InDutyQueue))
                return;

            if (!Throttler.Shared.Throttle("进入副本节流", 2000))
            {
                await Task.Delay(100, cancellationToken);
                continue;
            }

            if (!LuminaGetter.TryGetRow<TerritoryType>(ExecutorPreset!.Zone, out var zone))
            {
                await Task.Delay(100, cancellationToken);
                continue;
            }

            switch (runOptions.ContentEntryType)
            {
                case ContentEntryType.Normal:
                    ContentsFinderHelper.RequestDutyNormal(zone.ContentFinderCondition.RowId, runOptions.ContentsFinderOption);
                    break;
                case ContentEntryType.Support:
                    var supportRow = LuminaGetter.Get<DawnContent>().FirstOrDefault(x => x.Content.RowId == zone.ContentFinderCondition.RowId);

                    if (supportRow.RowId == 0)
                    {
                        NotifyHelper.Instance().Chat("无法找到对应的剧情辅助器副本, 请检查修正后重新运行");
                        return;
                    }

                    ContentsFinderHelper.RequestDutySupport(supportRow.RowId);
                    break;
            }

            if (DService.Instance().Condition.Any(ConditionFlag.WaitingForDutyFinder, ConditionFlag.WaitingForDuty, ConditionFlag.InDutyQueue))
                return;

            await Task.Delay(100, cancellationToken);
        }
    }

    private async Task OpenTreasuresAsync
    (
        CancellationToken cancellationToken
    )
    {
        var localPlayer = DService.Instance().ObjectTable.LocalPlayer;
        var originalPos = localPlayer?.Position ?? default;
        var settleDelay = 50;

        unsafe
        {
            if (LuminaGetter.TryGetRow<ContentFinderCondition>(GameMain.Instance()->CurrentContentFinderConditionId, out var data) &&
                data.ContentType.RowId is 4 or 5)
                settleDelay = 2300;
        }

        SetRunningMessage("搜寻宝箱中");

        var treasures = DService.Instance().ObjectTable.Where(obj => obj.ObjectKind == ObjectKind.Treasure).ToList();
        if (treasures.Count == 0)
            return;

        foreach (var treasure in treasures)
        {
            cancellationToken.ThrowIfCancellationRequested();

            MovementController.Teleport(treasure.Position);
            await Task.Delay(settleDelay, cancellationToken);

            await WaitUntilAsync
            (
                () =>
                {
                    if (!Throttler.Shared.Throttle("交互宝箱节流")) return false;
                    return treasure.TargetInteract();
                },
                "与宝箱交互",
                cancellationToken
            );
        }

        MovementController.Teleport(originalPos);
    }

    protected override void LeaveDuty() =>
        ExecuteCommandManager.Instance().ExecuteCommand
        (
            ExecuteCommandFlag.LeaveDuty,
            DService.Instance().Condition[ConditionFlag.InCombat] ?
                1U :
                0
        );

    protected override void SetRunningMessage
    (
        string message
    ) => RunningMessage = message;

    private void AbortPrevious()
    {
        CancelCurrentWork();
        Movement.Cancel();
    }

    private void CancelCurrentWork()
    {
        if (currentWorkCancellationSource is not { IsCancellationRequested: false } currentWorkCts)
            return;

        currentWorkCts.Cancel();
    }

    private void Finish
    (
        PresetExecutorResult result,
        bool                 abortQueue
    )
    {
        if (Result != null)
            return;

        ResetRuntimeCursor();
        Result = result;

        if (abortQueue)
            AbortPrevious();

        UnregisterListeners();
        completionSource.TrySetResult(result);
    }

    protected override ConditionContext CreateConditionContext() => ConditionContext.Create((int)CurrentRound);

    protected override void ValidateStepIndex
    (
        int stepIndex
    )
    {
        if (ExecutorPreset == null || stepIndex < 0 || stepIndex >= ExecutorPreset.Steps.Count)
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
}
