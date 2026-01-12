using System;
using System.Linq;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.ClientState.Objects.Enums;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.UI;
using Lumina.Excel.Sheets;
using UntarnishedHeart.Managers;
using UntarnishedHeart.Utils;
using UntarnishedHeart.Windows;

namespace UntarnishedHeart.Executor;

public class Executor : IDisposable
{
    private TaskHelper? taskHelper;

    public Executor(ExecutorPreset? preset, int maxRound = -1, DutyOptions? dutyOptions = null)
    {
        if (preset is not { IsValid: true }) return;

        taskHelper ??= new() { TimeoutMS = int.MaxValue, ShowDebug = true };

        DService.Instance().AddonLifecycle.RegisterListener(AddonEvent.PostDraw, "ContentsFinderConfirm", OnAddonDraw);

        DService.Instance().ClientState.TerritoryChanged += OnZoneChanged;

        DService.Instance().DutyState.DutyStarted     += OnDutyStarted;
        DService.Instance().DutyState.DutyRecommenced += OnDutyStarted;
        DService.Instance().DutyState.DutyCompleted   += OnDutyCompleted;

        MaxRound       = maxRound;
        ExecutorPreset = preset;

        // 使用传入的副本选项配置，如果没有传入则使用全局配置
        if (dutyOptions != null)
        {
            ContentsFinderOption = dutyOptions.ContentsFinderOption;
            ContentEntryType     = dutyOptions.ContentEntryType;
        }
        else
        {
            ContentsFinderOption = Service.Config.ContentsFinderOption;
            ContentEntryType     = Service.Config.ContentEntryType;
        }

        if (DService.Instance().ClientState.TerritoryType == ExecutorPreset.Zone)
            OnDutyStarted(null, DService.Instance().ClientState.TerritoryType);
        else if (!OccupiedInEvent && Service.Config.LeaderMode)
            EnqueueRegDuty();
    }

    public uint            CurrentRound   { get; private set; }
    public int             MaxRound       { get; init; }
    public ExecutorPreset? ExecutorPreset { get; init; }
    public string          RunningMessage => taskHelper?.CurrentTaskName ?? string.Empty;
    public bool            IsDisposed     { get; private set; }

    public bool IsFinished => CurrentRound == MaxRound;

    /// <summary>
    ///     副本选项配置
    /// </summary>
    public ContentsFinderOption ContentsFinderOption { get; init; }

    /// <summary>
    ///     副本进入类型
    /// </summary>
    public ContentEntryType ContentEntryType { get; init; }

    public void Dispose()
    {
        if (IsDisposed) return;

        DService.Instance().ClientState.TerritoryChanged -= OnZoneChanged;
        DService.Instance().DutyState.DutyCompleted      -= OnDutyCompleted;
        DService.Instance().DutyState.DutyStarted        -= OnDutyStarted;
        DService.Instance().DutyState.DutyRecommenced    -= OnDutyStarted;
        DService.Instance().AddonLifecycle.UnregisterListener(OnAddonDraw);

        taskHelper?.Abort();
        taskHelper?.Dispose();
        taskHelper = null;

        IsDisposed = true;
    }

    // 自动确认进入副本
    private static unsafe void OnAddonDraw(AddonEvent type, AddonArgs args)
    {
        if (!Throttler.Throttle("自动确认进入副本节流")) return;

        if (args.Addon == nint.Zero) return;
        args.Addon.ToStruct()->Callback(8);
    }

    // 确认进入副本区域
    private void OnZoneChanged(ushort zone)
    {
        if (ExecutorPreset == null || zone != ExecutorPreset.Zone) return;
        AbortPrevious();
    }

    // 副本开始 / 重新挑战
    private void OnDutyStarted(object? sender, ushort zone)
    {
        AbortPrevious();
        if (ExecutorPreset == null || zone != ExecutorPreset.Zone) return;

        if (Service.Config.AutoRecommendGear)
            taskHelper.Enqueue(GameFunctions.EquipRecommendGear, "尝试切换最强装备");

        EnqueuePreset();
    }

    // 填装预设步骤
    private void EnqueuePreset()
    {
        taskHelper.Enqueue(() => DService.Instance().ObjectTable.LocalPlayer != null && UIModule.IsScreenReady(), "等待区域加载结束");

        taskHelper.Enqueue
        (
            () =>
            {
                if (!Throttler.Throttle("等待副本开始节流")) return false;
                return DService.Instance().DutyState.IsDutyStarted;
            },
            "等待副本开始"
        );

        foreach (var task in ExecutorPreset.GetTasks(taskHelper))
            task.Invoke();
    }

    // 副本完成
    private void OnDutyCompleted(object? sender, ushort zone)
    {
        AbortPrevious();
        if (ExecutorPreset == null || zone != ExecutorPreset.Zone) return;

        if (ExecutorPreset.AutoOpenTreasures)
            EnqueueTreasureHunt();

        if (ExecutorPreset.DutyDelay > 0)
            taskHelper.DelayNext(ExecutorPreset.DutyDelay);

        taskHelper.Enqueue
        (() =>
            {
                GameFunctions.LeaveDuty();
                CurrentRound++;

                if (MaxRound != -1 && CurrentRound >= MaxRound)
                {
                    Dispose();
                    return;
                }

                if (!Service.Config.LeaderMode) return;
                EnqueueRegDuty();
            },
            "副本完成, 离开副本, 进入下一局"
        );
    }

    // 填装进入副本
    private void EnqueueRegDuty()
    {
        taskHelper.Enqueue
        (
            () =>
            {
                if (!Throttler.Throttle("等待副本结束节流")) return false;
                return !DService.Instance().DutyState.IsDutyStarted && DService.Instance().ClientState.TerritoryType != ExecutorPreset.Zone;
            },
            "等待副本结束"
        );

        taskHelper.Enqueue
        (
            () => DService.Instance().ObjectTable.LocalPlayer != null && !DService.Instance().Condition[ConditionFlag.BetweenAreas],
            "等待区域加载结束"
        );

        taskHelper.Enqueue
        (
            () =>
            {
                if (DService.Instance().Condition.Any(ConditionFlag.WaitingForDutyFinder, ConditionFlag.WaitingForDuty, ConditionFlag.InDutyQueue))
                    return true;

                if (!Throttler.Throttle("进入副本节流", 2000)) return false;
                if (!LuminaGetter.TryGetRow<TerritoryType>(ExecutorPreset.Zone, out var zone)) return false;

                switch (ContentEntryType)
                {
                    case ContentEntryType.Normal:
                        ContentsFinderHelper.RequestDutyNormal(zone.ContentFinderCondition.RowId, ContentsFinderOption);
                        break;
                    case ContentEntryType.Support:
                        var supportRow = LuminaGetter.Get<DawnContent>()
                                                     .FirstOrDefault(x => x.Content.RowId == zone.ContentFinderCondition.RowId);

                        if (supportRow.RowId == 0)
                        {
                            Chat("无法找到对应的剧情辅助器副本, 请检查修正后重新运行", Main.UTHPrefix);
                            return true;
                        }

                        ContentsFinderHelper.RequestDutySupport(supportRow.RowId);
                        break;
                }

                return DService.Instance().Condition.Any(ConditionFlag.WaitingForDutyFinder, ConditionFlag.WaitingForDuty, ConditionFlag.InDutyQueue);
            },
            "等待进入下一局"
        );
    }

    // 填装搜寻宝箱
    private unsafe void EnqueueTreasureHunt()
    {
        var localPlayer  = DService.Instance().ObjectTable.LocalPlayer;
        var origPosition = localPlayer?.Position ?? default;
        var setDelayTime = 50;

        if (LuminaGetter.TryGetRow<ContentFinderCondition>(GameMain.Instance()->CurrentContentFinderConditionId, out var data) &&
            data.ContentType.RowId is 4 or 5)
            setDelayTime = 2300;

        taskHelper.Enqueue
        (
            () =>
            {
                var treasures = DService.Instance().ObjectTable
                                        .Where(obj => obj.ObjectKind == ObjectKind.Treasure)
                                        .ToList();
                if (treasures.Count == 0) return false;

                foreach (var obj in treasures)
                {
                    taskHelper.Enqueue
                    (
                        () =>
                        {
                            GameFunctions.Teleport(obj.Position);
                            localPlayer.ToStruct()->RotationModified();
                        },
                        "传送至宝箱",
                        weight: 2
                    );
                    taskHelper.DelayNext(setDelayTime, "等待位置确认", 2);
                    taskHelper.Enqueue
                    (
                        () =>
                        {
                            if (!Throttler.Throttle("交互宝箱节流")) return false;
                            return obj.TargetInteract();
                        },
                        "与宝箱交互",
                        weight: 2
                    );
                }

                return true;
            },
            "搜寻宝箱中"
        );

        taskHelper.Enqueue(() => GameFunctions.Teleport(origPosition), "传送回原始位置");
    }

    // 放弃先前任务
    private void AbortPrevious()
    {
        taskHelper.Abort();
        GameFunctions.PathFindCancel();
    }

    public void ManualEnqueueNewRound()
    {
        AbortPrevious();
        taskHelper.Enqueue
        (() =>
            {
                GameFunctions.LeaveDuty();
                CurrentRound++;

                if (MaxRound != -1 && CurrentRound >= MaxRound)
                {
                    Dispose();
                    return;
                }

                if (!Service.Config.LeaderMode) return;
                EnqueueRegDuty();
            },
            "手动退出副本开启新一局"
        );
    }
}
