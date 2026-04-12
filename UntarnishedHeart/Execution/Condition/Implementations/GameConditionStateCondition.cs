using System.Collections.Frozen;
using Dalamud.Game.ClientState.Conditions;
using Newtonsoft.Json;
using UntarnishedHeart.Execution.Condition.Configuration;
using UntarnishedHeart.Execution.Condition.Enums;
using UntarnishedHeart.Windows;

namespace UntarnishedHeart.Execution.Condition;

[JsonObject(MemberSerialization.OptIn)]
[ConditionJsonType("GameCondition", ConditionDetectType.GameCondition)]
public sealed class GameConditionStateCondition : ConditionBase
{
    public override ConditionDetectType Kind => ConditionDetectType.GameCondition;

    [JsonProperty("Flag")]
    public ConditionFlag Flag { get; set; } = ConditionFlag.InCombat;

    [JsonProperty("ComparisonType")]
    public PresenceComparisonType ComparisonType { get; set; } = PresenceComparisonType.NotHas;

    public override bool Evaluate(in ConditionContext context)
    {
        var hasFlag = DService.Instance().Condition[Flag];
        return ComparisonType == PresenceComparisonType.Has ? hasFlag : !hasFlag;
    }

    protected override bool EqualsCore(ConditionBase other) =>
        other is GameConditionStateCondition condition &&
        Flag           == condition.Flag               &&
        ComparisonType == condition.ComparisonType;

    protected override int GetCoreHashCode() => HashCode.Combine((int)Flag, (int)ComparisonType);

    public override ConditionBase DeepCopy() =>
        CopyBasePropertiesTo
        (
            new GameConditionStateCondition
            {
                Flag           = Flag,
                ComparisonType = ComparisonType
            }
        );

    protected override void DrawBody()
    {
        DrawLabel("比较类型", KnownColor.LightSkyBlue.ToVector4());
        var comparisonCandidates = Enum.GetValues<PresenceComparisonType>();
        using (var combo = ImRaii.Combo("###ComparisonTypeCombo", ComparisonType.GetDescription(), ImGuiComboFlags.HeightLargest))
        {
            if (combo)
                ImGui.CloseCurrentPopup();
        }

        if (ImGui.IsItemClicked())
        {
            CollectionSelectorWindow.OpenEnum
            (
                "选择比较类型",
                "暂无可选比较类型",
                ComparisonType,
                value => ComparisonType = value,
                comparisonCandidates
            );
        }

        DrawLabel("状态标记", KnownColor.LightSkyBlue.ToVector4());
        var flagCandidates = Enum
                             .GetValues<ConditionFlag>()
                             .Where(static value => value != ConditionFlag.None)
                             .ToArray();
        using (var combo = ImRaii.Combo("###ConditionFlagCombo", GetConditionFlagDisplayText(Flag), ImGuiComboFlags.HeightLargest))
        {
            if (combo)
                ImGui.CloseCurrentPopup();
        }

        if (ImGui.IsItemClicked())
        {
            CollectionSelectorWindow.OpenEnum
            (
                "选择状态标记",
                "暂无可选状态标记",
                Flag,
                static candidate => new CollectionSelectorItem(GetConditionFlagDisplayText(candidate), GetConditionFlagDescription(candidate)),
                value => Flag = value,
                flagCandidates
            );
        }
    }

    private static string GetConditionFlagDisplayText(ConditionFlag flag) =>
        $"{GetConditionFlagChineseName(flag)} ({flag})";

    private static string GetConditionFlagDescription(ConditionFlag flag) =>
        $"枚举名: {flag}";

    private static string GetConditionFlagChineseName(ConditionFlag flag) =>
        ConditionFlagTranslations.TryGetValue(flag, out var translation)
            ? translation
            : throw new InvalidOperationException($"未配置 ConditionFlag {flag} 的中文翻译");

    #region 常量

    private static readonly FrozenDictionary<ConditionFlag, string> ConditionFlagTranslations = new Dictionary<ConditionFlag, string>
    {
        [ConditionFlag.None]                                     = "无",
        [ConditionFlag.NormalConditions]                         = "通常状态",
        [ConditionFlag.Unconscious]                              = "无法行动中",
        [ConditionFlag.Emoting]                                  = "情感动作中",
        [ConditionFlag.Mounted]                                  = "骑乘坐骑中",
        [ConditionFlag.Crafting]                                 = "制作中",
        [ConditionFlag.Gathering]                                = "采集中",
        [ConditionFlag.MeldingMateria]                           = "镶嵌魔晶石中",
        [ConditionFlag.OperatingSiegeMachine]                    = "操作固定场景物体中",
        [ConditionFlag.CarryingObject]                           = "搬运物体中",
        [ConditionFlag.RidingPillion]                            = "共同骑乘中",
        [ConditionFlag.InThatPosition]                           = "姿势受限中",
        [ConditionFlag.ChocoboRacing]                            = "陆行鸟竞赛中",
        [ConditionFlag.PlayingMiniGame]                          = "游玩小游戏中",
        [ConditionFlag.PlayingLordOfVerminion]                   = "萌宠之王中",
        [ConditionFlag.ParticipatingInCustomMatch]               = "自定义比赛中",
        [ConditionFlag.Performing]                               = "演奏中",
        [ConditionFlag.Occupied]                                 = "忙碌中",
        [ConditionFlag.InCombat]                                 = "战斗中",
        [ConditionFlag.Casting]                                  = "咏唱中",
        [ConditionFlag.SufferingStatusAffliction]                = "受异常状态效果影响中",
        [ConditionFlag.SufferingStatusAffliction2]               = "受异常状态效果影响中 2",
        [ConditionFlag.Occupied30]                               = "忙碌中 30",
        [ConditionFlag.OccupiedInEvent]                          = "因事件忙碌中",
        [ConditionFlag.OccupiedInQuestEvent]                     = "因任务事件忙碌中",
        [ConditionFlag.Occupied33]                               = "忙碌中 33",
        [ConditionFlag.BoundByDuty]                              = "副本中",
        [ConditionFlag.OccupiedInCutSceneEvent]                  = "因剧情事件忙碌中",
        [ConditionFlag.InDuelingArea]                            = "决斗区域中",
        [ConditionFlag.TradeOpen]                                = "交易中",
        [ConditionFlag.Occupied38]                               = "忙碌中 38",
        [ConditionFlag.Occupied39]                               = "忙碌中 39",
        [ConditionFlag.ExecutingCraftingAction]                  = "使用制作技能中",
        [ConditionFlag.PreparingToCraft]                         = "准备制作中",
        [ConditionFlag.ExecutingGatheringAction]                 = "使用采集技能中",
        [ConditionFlag.Fishing]                                  = "钓鱼中",
        [ConditionFlag.BetweenAreas]                             = "区域切换中",
        [ConditionFlag.Stealthed]                                = "隐身中",
        [ConditionFlag.Jumping]                                  = "跳跃中",
        [ConditionFlag.UsingChocoboTaxi]                         = "使用陆行鸟运输中",
        [ConditionFlag.OccupiedSummoningBell]                    = "使用雇员铃中",
        [ConditionFlag.BetweenAreas51]                           = "区域切换中 51",
        [ConditionFlag.SystemError]                              = "系统错误",
        [ConditionFlag.LoggingOut]                               = "登出中",
        [ConditionFlag.ConditionLocation]                        = "当前位置受限",
        [ConditionFlag.WaitingForDuty]                           = "等待进入副本中",
        [ConditionFlag.BoundByDuty56]                            = "副本中 56",
        [ConditionFlag.MountOrOrnamentTransition]                = "切换坐骑或时尚配饰中",
        [ConditionFlag.WatchingCutscene]                         = "观看过场剧情中",
        [ConditionFlag.WaitingForDutyFinder]                     = "等待确认进入副本",
        [ConditionFlag.CreatingCharacter]                        = "创建角色中",
        [ConditionFlag.Jumping61]                                = "跳跃中 61",
        [ConditionFlag.PvPDisplayActive]                         = "玩家对战界面打开中",
        [ConditionFlag.SufferingStatusAffliction63]              = "受异常状态效果影响中 63",
        [ConditionFlag.Mounting]                                 = "召唤坐骑中",
        [ConditionFlag.CarryingItem]                             = "搬运道具中",
        [ConditionFlag.UsingPartyFinder]                         = "使用玩家招募中",
        [ConditionFlag.UsingHousingFunctions]                    = "使用房屋功能中",
        [ConditionFlag.Transformed]                              = "变身中",
        [ConditionFlag.OnFreeTrial]                              = "免费试玩中",
        [ConditionFlag.BeingMoved]                               = "强制移动中",
        [ConditionFlag.Mounting71]                               = "召唤坐骑中 71",
        [ConditionFlag.SufferingStatusAffliction72]              = "受异常状态效果影响中 72",
        [ConditionFlag.SufferingStatusAffliction73]              = "受异常状态效果影响中 73",
        [ConditionFlag.RegisteringForRaceOrMatch]                = "报名竞赛或比赛中",
        [ConditionFlag.WaitingForRaceOrMatch]                    = "等待竞赛或比赛中",
        [ConditionFlag.WaitingForTripleTriadMatch]               = "等待九宫幻卡比赛中",
        [ConditionFlag.InFlight]                                 = "飞行中",
        [ConditionFlag.WatchingCutscene78]                       = "观看过场剧情中 78",
        [ConditionFlag.InDeepDungeon]                            = "深层迷宫中",
        [ConditionFlag.Swimming]                                 = "游泳中",
        [ConditionFlag.Diving]                                   = "潜水中",
        [ConditionFlag.RegisteringForTripleTriadMatch]           = "报名九宫幻卡比赛中",
        [ConditionFlag.WaitingForTripleTriadMatch83]             = "等待九宫幻卡比赛中 83",
        [ConditionFlag.ParticipatingInCrossWorldPartyOrAlliance] = "跨服组队中",
        [ConditionFlag.Unknown85]                                = "未知状态 85",
        [ConditionFlag.DutyRecorderPlayback]                     = "观看回放中",
        [ConditionFlag.Casting87]                                = "咏唱中 87",
        [ConditionFlag.MountImmobile]                            = "坐骑不可移动",
        [ConditionFlag.InThisState89]                            = "状态受限 89",
        [ConditionFlag.RolePlaying]                              = "角色扮演中",
        [ConditionFlag.InDutyQueue]                              = "匹配副本中",
        [ConditionFlag.ReadyingVisitOtherWorld]                  = "跨界传送准备就绪",
        [ConditionFlag.WaitingToVisitOtherWorld]                 = "等待跨界传送中",
        [ConditionFlag.UsingFashionAccessory]                    = "使用时尚配饰中",
        [ConditionFlag.BoundByDuty95]                            = "副本中 95",
        [ConditionFlag.Unknown96]                                = "未知状态 96",
        [ConditionFlag.Disguised]                                = "伪装中",
        [ConditionFlag.RecruitingWorldOnly]                      = "服务器内招募中",
        [ConditionFlag.Unknown99]                                = "未知状态 99",
        [ConditionFlag.EditingPortrait]                          = "编辑肖像中",
        [ConditionFlag.Unknown101]                               = "未知状态 101",
        [ConditionFlag.PilotingMech]                             = "驾驶机甲中",
        [ConditionFlag.EditingStrategyBoard]                     = "编辑战术板中"
    }.ToFrozenDictionary();

    #endregion
}
