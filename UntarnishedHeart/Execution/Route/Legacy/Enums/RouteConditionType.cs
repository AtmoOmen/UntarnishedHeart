using System.ComponentModel;

namespace UntarnishedHeart.Execution.Route.Legacy.Enums;

/// <summary>
///     路线条件判断类型
/// </summary>
public enum RouteConditionType
{
    [Description("玩家等级")]
    PlayerLevel,

    [Description("最优队员推荐")]
    OptimalPartyRecommendation,

    [Description("本轮已完成副本次数 (运行预设实际变更时清零)")]
    CompletedDutyCount,

    [Description("成就数")]
    AchievementCount,

    [Description("物品个数")]
    ItemCount
}
