using System.Numerics;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.ClientState.Objects.Enums;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using Lumina.Excel.Sheets;
using OmenTools.Dalamud;
using OmenTools.Info.Game.Enums;
using OmenTools.Interop.Game;
using OmenTools.Interop.Game.Helpers;
using OmenTools.Interop.Game.Lumina;
using OmenTools.Interop.Windows.Helpers;
using OmenTools.OmenService;
using OmenTools.Threading;
using UntarnishedHeart.Execution.CommandCondition.Enums;
using UntarnishedHeart.Execution.Enums;

namespace UntarnishedHeart.Execution.Preset;

public class PresetExecutor : IDisposable
{
    private readonly SemaphoreSlim                              manualInteractGate = new(1, 1);
    private readonly TaskCompletionSource<PresetExecutorResult> completionSource   = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly PresetExecutorRunOptions                   runOptions;

    private CancellationTokenSource? executorCancellationSource;
    private CancellationTokenSource? currentWorkCancellationSource;
    private Task?                    currentWorkTask;
    private CancellationTokenSource? movementCancellationSource;
    private Task?                    movementTask;
    private bool                     isStarted;
    private bool                     listenersRegistered;

    internal PresetExecutor(Preset? preset, PresetExecutorRunOptions runOptions)
    {
        ExecutorPreset  = preset;
        this.runOptions = runOptions;
    }

    public uint CurrentRound { get; private set; }

    public int MaxRound => runOptions.MaxRound;

    public Preset? ExecutorPreset { get; }

    public string RunningMessage { get; private set; } = string.Empty;

    public bool IsDisposed { get; private set; }

    public bool IsFinished => Result?.EndReason == ExecutorEndReason.Completed;

    public bool IsStopped => Result?.EndReason == ExecutorEndReason.Stopped;

    internal Task<PresetExecutorResult> Completion => completionSource.Task;

    internal PresetExecutorResult? Result { get; private set; }

    internal PresetExecutorProgress Progress =>
        new()
        {
            CurrentRound   = CurrentRound,
            MaxRound       = MaxRound,
            RunningMessage = RunningMessage,
            IsFinished     = IsFinished,
            IsStopped      = IsStopped
        };

    public void Start()
    {
        if (IsDisposed || isStarted || Completion.IsCompleted)
            return;

        isStarted = true;

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
            OnDutyStarted(null, DService.Instance().ClientState.TerritoryType);
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

        movementCancellationSource?.Dispose();
        movementCancellationSource = null;

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

        ReplaceCurrentWork(token => LeaveDutyAndAdvanceRoundAsync("手动退出副本开启新一局", token));
    }

    public bool RequestNearestInteract()
    {
        if (Completion.IsCompleted || IsDisposed)
            return false;

        _ = RunManualNearestInteractAsync();
        return true;
    }

    private async Task RunManualNearestInteractAsync()
    {
        if (!await manualInteractGate.WaitAsync(0))
            return;

        try
        {
            using var commandCancellationSource = executorCancellationSource == null
                                                      ? null
                                                      : CancellationTokenSource.CreateLinkedTokenSource(executorCancellationSource.Token);

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

    private static unsafe void OnAddonDraw(AddonEvent type, AddonArgs args)
    {
        if (!Throttler.Shared.Throttle("自动确认进入副本节流")) return;

        if (args.Addon == nint.Zero) return;
        args.Addon.ToStruct()->Callback(8);
    }

    private void OnZoneChanged(ushort zone)
    {
        if (ExecutorPreset == null || zone != ExecutorPreset.Zone || Completion.IsCompleted)
            return;

        AbortPrevious();
    }

    private void OnDutyStarted(object? sender, ushort zone)
    {
        if (ExecutorPreset == null || zone != ExecutorPreset.Zone || Completion.IsCompleted)
            return;

        ReplaceCurrentWork(RunPresetAsync);
    }

    private void OnDutyCompleted(object? sender, ushort zone)
    {
        if (ExecutorPreset == null || zone != ExecutorPreset.Zone || Completion.IsCompleted)
            return;

        ReplaceCurrentWork(HandleDutyCompletedAsync);
    }

    private void ReplaceCurrentWork(Func<CancellationToken, Task> workFactory)
    {
        AbortPrevious();

        if (Completion.IsCompleted || executorCancellationSource is not { IsCancellationRequested: false } executorCts)
            return;

        var workCancellationSource = CancellationTokenSource.CreateLinkedTokenSource(executorCts.Token);
        currentWorkCancellationSource = workCancellationSource;

        currentWorkTask = Task.Run
        (async () =>
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
            }
        );
    }

    private async Task RunPresetAsync(CancellationToken cancellationToken)
    {
        await WaitUntilAsync(() => DService.Instance().DutyState.IsDutyStarted, "等待副本开始", cancellationToken);

        if (runOptions.AutoRecommendGear)
            await EquipRecommendedGearAsync(cancellationToken);

        for (var stepIndex = 0; stepIndex < ExecutorPreset!.Steps.Count;)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var step = ExecutorPreset.Steps[stepIndex];

            await ExecuteStepAsync(step, cancellationToken);

            if (step.JumpToIndex >= 0 && step.JumpToIndex < ExecutorPreset.Steps.Count)
                stepIndex = step.JumpToIndex;
            else
                stepIndex++;
        }

        SetRunningMessage("等待副本完成");
    }

    private async Task HandleDutyCompletedAsync(CancellationToken cancellationToken)
    {
        if (ExecutorPreset!.AutoOpenTreasures)
            await OpenTreasuresAsync(cancellationToken);

        if (ExecutorPreset.DutyDelay > 0)
            await DelayAsync(ExecutorPreset.DutyDelay, $"等待退出延迟: {ExecutorPreset.DutyDelay} ms", cancellationToken);

        await LeaveDutyAndAdvanceRoundAsync("副本完成, 离开副本, 进入下一局", cancellationToken);
    }

    private async Task ExecuteStepAsync(PresetStep step, CancellationToken cancellationToken)
    {
        await WaitStepPreconditionsAsync(step, cancellationToken);
        await ExecuteStepMovementAsync(step, cancellationToken);

        if (step.InteractWithNearestObject)
            await ExecuteNearestInteractAsync(step.Note, cancellationToken);
        else
            await ExecuteSpecificTargetActionsAsync(step, cancellationToken);

        await ExecuteTextCommandsAsync(step, cancellationToken);

        if (step.Delay > 0)
            await DelayAsync((int)step.Delay, $"等待 {step.Delay} ms: {step.Note}", cancellationToken);
    }

    private async Task WaitStepPreconditionsAsync(PresetStep step, CancellationToken cancellationToken)
    {
        await WaitUntilAsync
        (
            () =>
            {
                if (!step.StopWhenAnyAlive || DService.Instance().PartyList.Length < 2) return true;
                if (DService.Instance().ObjectTable.LocalPlayer is not { } localPlayer) return false;

                foreach (var member in DService.Instance().PartyList)
                {
                    if (member.EntityId  == localPlayer.EntityID) continue;
                    if (member.CurrentHP != 0) return false;
                }

                return true;
            },
            $"检查队友存活状态: {step.Note}",
            cancellationToken
        );

        await WaitUntilAsync
        (
            () => !step.StopInCombat || !DService.Instance().Condition[ConditionFlag.InCombat],
            $"检查进战状态: {step.Note}",
            cancellationToken
        );

        await WaitUntilAsync
        (
            () => !step.StopWhenBusy || !DService.Instance().Condition.IsOccupiedInEvent && UIModule.IsScreenReady(),
            $"检查忙碌状态: {step.Note}",
            cancellationToken
        );
    }

    private async Task ExecuteStepMovementAsync(PresetStep step, CancellationToken cancellationToken)
    {
        if (step.Position == default)
            return;

        var finalMoveType = step.MoveType == MoveType.无 ? MoveType.传送 : step.MoveType;

        switch (finalMoveType)
        {
            case MoveType.寻路:
                StartPathfindMovement(step.Position, cancellationToken);
                break;
            case MoveType.vnavmesh:
                StartVnavmeshMovement(step.Position, cancellationToken);
                break;
            case MoveType.传送:
            case MoveType.无:
            default:
                Teleport(step.Position);
                break;
        }

        if (!step.WaitForGetClose)
            return;

        await WaitUntilAsync
        (
            () => DService.Instance().ObjectTable.LocalPlayer is { } localPlayer &&
                  Vector2.DistanceSquared(localPlayer.Position.ToVector2(), step.Position.ToVector2()) <= 4f,
            $"等待完全接近目标位置: {step.Note}",
            cancellationToken
        );
    }

    private async Task ExecuteSpecificTargetActionsAsync(PresetStep step, CancellationToken cancellationToken)
    {
        if (step.DataID == 0)
            return;

        if (step.WaitForTargetSpawn)
        {
            await WaitUntilAsync
            (
                () =>
                {
                    if (!Throttler.Shared.Throttle("等待目标生成节流")) return false;
                    return step.FindObject() != null;
                },
                $"等待目标生成: {step.Note}",
                cancellationToken
            );
        }

        if (step.WaitForTarget)
        {
            await WaitUntilAsync
            (
                () =>
                {
                    if (!Throttler.Shared.Throttle("选中目标节流")) return false;
                    step.TargetObject();
                    return TargetManager.Target != null;
                },
                $"选中目标: {step.Note}",
                cancellationToken
            );
        }

        if (!step.InteractWithTarget)
            return;

        await WaitUntilAsync
        (
            () =>
            {
                if (step.InteractNeedTargetAnything && TargetManager.Target is null)
                    return true;

                if (step.FindObject() is not { } validObject)
                    return true;

                return validObject.TargetInteract();
            },
            $"交互预设目标: {step.Note}",
            cancellationToken
        );

        await DelayAsync(200, $"等待交互开始: {step.Note}", cancellationToken);
        await WaitUntilAsync
        (
            () => !DService.Instance().Condition.IsOccupiedInEvent && UIModule.IsScreenReady(),
            $"等待目标交互完成: {step.Note}",
            cancellationToken
        );
    }

    private async Task ExecuteTextCommandsAsync(PresetStep step, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(step.Commands))
            return;

        if (step.CommandCondition.Conditions.Count == 0)
        {
            await RunCommandsAsync(step.Commands, step.Note, cancellationToken);
            return;
        }

        await WaitUntilAsync
        (
            () => DService.Instance().ObjectTable.LocalPlayer != null,
            $"执行文本指令时本地玩家不能为空: {step.Note}",
            cancellationToken
        );

        switch (step.CommandCondition.ExecuteType)
        {
            case CommandExecuteType.Pass:
                if (step.CommandCondition.IsConditionsTrue())
                    await RunCommandsAsync(step.Commands, step.Note, cancellationToken);
                break;

            case CommandExecuteType.Wait:
                await WaitUntilAsync
                (
                    step.CommandCondition.IsConditionsTrue,
                    $"等待文本指令条件变成真: {step.Note}",
                    cancellationToken
                );
                await RunCommandsAsync(step.Commands, step.Note, cancellationToken);
                break;

            case CommandExecuteType.Repeat:
                while (!step.CommandCondition.IsConditionsTrue())
                {
                    await RunCommandsAsync(step.Commands, step.Note, cancellationToken);

                    if (step.CommandCondition.TimeValue > 0)
                        await DelayAsync((int)step.CommandCondition.TimeValue, $"文本指令重复间隔: {step.Note}", cancellationToken);
                }

                await RunCommandsAsync(step.Commands, step.Note, cancellationToken);
                break;
        }
    }

    private async Task RunCommandsAsync(string commands, string stepNote, CancellationToken cancellationToken)
    {
        foreach (var command in commands.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (command.StartsWith("/wait", StringComparison.OrdinalIgnoreCase))
            {
                var split = command.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

                if (split.Length == 2 && uint.TryParse(split[1], out var waitTime))
                {
                    await DelayAsync((int)waitTime, $"特殊文本指令 {command}: {stepNote}", cancellationToken);
                    continue;
                }
            }

            SetRunningMessage($"使用文本指令: {stepNote} {command}");
            ChatManager.Instance().SendCommand(command);
            await Task.Delay(100, cancellationToken);
        }
    }

    private async Task ExecuteNearestInteractAsync(string sourceName, CancellationToken cancellationToken)
    {
        var target = PresetStep.FindNearestInteractableObject();

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

        PresetStep.OpenObjectInteraction(target);
    }

    private async Task WaitUntilAsync(Func<bool> predicate, string message, CancellationToken cancellationToken, int intervalMs = 100)
    {
        SetRunningMessage(message);

        while (!predicate())
            await Task.Delay(intervalMs, cancellationToken);
    }

    private async Task DelayAsync(int delayMs, string message, CancellationToken cancellationToken)
    {
        SetRunningMessage(message);
        await Task.Delay(delayMs, cancellationToken);
    }

    private async Task EquipRecommendedGearAsync(CancellationToken cancellationToken)
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

    private async Task LeaveDutyAndAdvanceRoundAsync(string message, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        SetRunningMessage(message);
        LeaveDuty();
        CurrentRound++;

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

    private bool HasReachedMaxRound() =>
        MaxRound != -1 && CurrentRound >= MaxRound;

    private async Task RegisterDutyAsync(CancellationToken cancellationToken)
    {
        await WaitUntilAsync
        (
            () =>
            {
                if (!Throttler.Shared.Throttle("等待副本结束节流")) return false;
                return !DService.Instance().DutyState.IsDutyStarted && DService.Instance().ClientState.TerritoryType != ExecutorPreset!.Zone;
            },
            "等待副本结束",
            cancellationToken
        );

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
                    var supportRow = LuminaGetter.Get<DawnContent>()
                                                 .FirstOrDefault(x => x.Content.RowId == zone.ContentFinderCondition.RowId);

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

    private async Task OpenTreasuresAsync(CancellationToken cancellationToken)
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

            Teleport(treasure.Position);
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

        Teleport(originalPos);
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

        movementTask = Task.Run
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

    private static void LeaveDuty() =>
        ExecuteCommandManager.Instance().ExecuteCommand(ExecuteCommandFlag.LeaveDuty, DService.Instance().Condition[ConditionFlag.InCombat] ? 1U : 0);

    private void SetRunningMessage(string message) =>
        RunningMessage = message;

    private void AbortPrevious()
    {
        CancelCurrentWork();
        CancelMovement();
    }

    private void CancelCurrentWork()
    {
        if (currentWorkCancellationSource is not { IsCancellationRequested: false } currentWorkCts)
            return;

        currentWorkCts.Cancel();
    }

    private void CancelMovement()
    {
        if (movementCancellationSource is not { IsCancellationRequested: false } movementCts)
            return;

        movementCts.Cancel();
    }

    private void Finish(PresetExecutorResult result, bool abortQueue)
    {
        if (Result != null)
            return;

        Result = result;

        if (abortQueue)
            AbortPrevious();

        UnregisterListeners();
        completionSource.TrySetResult(result);
    }
}
