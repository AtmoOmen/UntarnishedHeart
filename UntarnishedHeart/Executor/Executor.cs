using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.ClientState.Conditions;
using System.Linq;
using System;
using Dalamud.Game.ClientState.Objects.Enums;
using FFXIVClientStructs.FFXIV.Client.Game;
using Lumina.Excel.Sheets;
using UntarnishedHeart.Managers;
using UntarnishedHeart.Utils;
using UntarnishedHeart.Windows;

namespace UntarnishedHeart.Executor;

public class Executor : IDisposable
{
    public uint            CurrentRound     { get; private set; }
    public int             MaxRound         { get; init; }
    public ExecutorPreset? ExecutorPreset   { get; init; }
    public string          RunningMessage   => TaskHelper?.CurrentTaskName ?? string.Empty;
    public bool            IsDisposed       { get; private set; }
    
    public bool IsFinished => CurrentRound == MaxRound;

    private TaskHelper? TaskHelper;

    /// <summary>
    /// 副本选项配置
    /// </summary>
    public ContentsFinderOption ContentsFinderOption { get; init; }
    
    /// <summary>
    /// 副本进入类型
    /// </summary>
    public ContentEntryType ContentEntryType { get; init; }

    public Executor(ExecutorPreset? preset, int maxRound = -1, DutyOptions? dutyOptions = null)
    {
        if (preset is not { IsValid: true }) return;

        TaskHelper ??= new() { TimeLimitMS = int.MaxValue };

        DService.AddonLifecycle.RegisterListener(AddonEvent.PostDraw, "ContentsFinderConfirm", OnAddonDraw);

        DService.ClientState.TerritoryChanged += OnZoneChanged;

        DService.DutyState.DutyStarted     += OnDutyStarted;
        DService.DutyState.DutyRecommenced += OnDutyStarted;
        DService.DutyState.DutyCompleted   += OnDutyCompleted;

        MaxRound       = maxRound;
        ExecutorPreset = preset;
        
        // 使用传入的副本选项配置，如果没有传入则使用全局配置
        if (dutyOptions != null)
        {
            ContentsFinderOption = dutyOptions.ContentsFinderOption;
            ContentEntryType = dutyOptions.ContentEntryType;
        }
        else
        {
            ContentsFinderOption = Service.Config.ContentsFinderOption;
            ContentEntryType = Service.Config.ContentEntryType;
        }

        if (DService.ClientState.TerritoryType == ExecutorPreset.Zone)
            OnDutyStarted(null, DService.ClientState.TerritoryType);
        else if (!OccupiedInEvent && Service.Config.LeaderMode)
            EnqueueRegDuty();
    }

    public void Dispose()
    {
        if (IsDisposed) return;

        DService.ClientState.TerritoryChanged -= OnZoneChanged;
        DService.DutyState.DutyCompleted      -= OnDutyCompleted;
        DService.DutyState.DutyStarted        -= OnDutyStarted;
        DService.DutyState.DutyRecommenced    -= OnDutyStarted;
        DService.AddonLifecycle.UnregisterListener(OnAddonDraw);

        TaskHelper?.Abort();
        TaskHelper?.Dispose();
        TaskHelper = null;

        IsDisposed = true;
    }

    // 自动确认进入副本
    private static unsafe void OnAddonDraw(AddonEvent type, AddonArgs args)
    {
        if (!Throttler.Throttle("自动确认进入副本节流")) return;

        if (args.Addon == nint.Zero) return;
        Callback(args.Addon.ToAtkUnitBase(), true, 8);
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
            TaskHelper.Enqueue(GameFunctions.EquipRecommendGear, "尝试切换最强装备");

        EnqueuePreset();
    }

    // 填装预设步骤
    private void EnqueuePreset()
    {
        TaskHelper.Enqueue(() => DService.ObjectTable.LocalPlayer != null && IsScreenReady(), "等待区域加载结束");

        TaskHelper.Enqueue(() =>
        {
            if (!Throttler.Throttle("等待副本开始节流")) return false;
            return DService.DutyState.IsDutyStarted;
        }, "等待副本开始");

        foreach (var task in ExecutorPreset.GetTasks(TaskHelper))
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
            TaskHelper.DelayNext(ExecutorPreset.DutyDelay);

        TaskHelper.Enqueue(() =>
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
        });
    }

    // 填装进入副本
    private void EnqueueRegDuty()
    {
        TaskHelper.Enqueue(() =>
        {
            if (!Throttler.Throttle("等待副本结束节流")) return false;
            return !DService.DutyState.IsDutyStarted && DService.ClientState.TerritoryType != ExecutorPreset.Zone;
        }, "等待副本结束");

        TaskHelper.Enqueue(
            () => DService.ObjectTable.LocalPlayer != null && !DService.Condition[ConditionFlag.BetweenAreas],
            "等待区域加载结束");

        TaskHelper.Enqueue(() =>
        {
            if (DService.Condition.Any(ConditionFlag.WaitingForDutyFinder, ConditionFlag.WaitingForDuty, ConditionFlag.InDutyQueue))
            {
                return true;
            }

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

            return DService.Condition.Any(ConditionFlag.WaitingForDutyFinder, ConditionFlag.WaitingForDuty, ConditionFlag.InDutyQueue);
        }, "等待进入下一局");
    }

    // 填装搜寻宝箱
    private unsafe void EnqueueTreasureHunt()
    {
        var localPlayer  = DService.ObjectTable.LocalPlayer;
        var origPosition = localPlayer?.Position ?? default;
        var setDelayTime = 50;

        if (LuminaGetter.TryGetRow<ContentFinderCondition>(GameMain.Instance()->CurrentContentFinderConditionId, out var data) &&
            data.ContentType.RowId is 4 or 5)
            setDelayTime = 2300;

        TaskHelper.Enqueue(() =>
        {
            var treasures = DService.ObjectTable
                                    .Where(obj => obj.ObjectKind == ObjectKind.Treasure)
                                    .ToList();
            if (treasures.Count == 0) return false;

            foreach (var obj in treasures)
            {
                TaskHelper.Enqueue(() =>
                {
                    GameFunctions.Teleport(obj.Position);
                    localPlayer.ToStruct()->RotationModified();
                }, "传送至宝箱", null, null, 2);
                TaskHelper.DelayNext(setDelayTime, "等待位置确认", false, 2);
                TaskHelper.Enqueue(() =>
                {
                    if (!Throttler.Throttle("交互宝箱节流")) return false;
                    return obj.TargetInteract();
                }, "与宝箱交互", null, null, 2);
            }

            return true;
        }, "搜寻宝箱中");

        TaskHelper.Enqueue(() => GameFunctions.Teleport(origPosition), "传送回原始位置");
    }

    // 放弃先前任务
    private void AbortPrevious()
    {
        TaskHelper.Abort();
        GameFunctions.PathFindCancel();
    }

    public void ManualEnqueueNewRound()
    {
        AbortPrevious();
        TaskHelper.Enqueue(() =>
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
        });
    }
}
