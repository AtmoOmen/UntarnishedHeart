using System.ComponentModel;

namespace UntarnishedHeart.Execution.Condition.Enums;

public enum ConditionDetectType
{
    [Description("游戏状态")]
    GameCondition,

    [Description("状态效果")]
    Status,

    [Description("体力")]
    Health,

    [Description("正在咏唱技能")]
    ActionCast,

    [Description("正在咏唱技能")]
    ActionCastStart = ActionCast,

    [Description("自身技能冷却")]
    ActionCooldown,

    [Description("技能是否可用")]
    ActionUsable,

    [Description("坐标范围")]
    PositionRange,

    [Description("周围存在特定目标")]
    NearbyTarget,

    [Description("自身是否有目标")]
    HasTarget,

    [Description("自身是否有特定目标")]
    HasSpecificTarget,

    [Description("当前是否队友均处于无法战斗状态")]
    PartyAllDead,

    [Description("当前是否已打开指定界面")]
    AddonOpened,

    [Description("当前队友等级")]
    PartyMembersLevel,

    [Description("当前目标的目标是否为自身")]
    TargetTargetIsSelf,

    [Description("玩家等级")]
    PlayerLevel,

    [Description("最优队员推荐")]
    OptimalPartyRecommendation,

    [Description("本轮已完成副本次数")]
    CompletedDutyCount,

    [Description("成就数")]
    AchievementCount,

    [Description("物品个数")]
    ItemCount
}
