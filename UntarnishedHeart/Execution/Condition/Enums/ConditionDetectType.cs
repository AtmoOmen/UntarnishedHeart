using System.ComponentModel;

namespace UntarnishedHeart.Execution.Condition.Enums;

public enum ConditionDetectType
{
    [Description("体力")]
    Health,

    [Description("状态效果")]
    Status,

    [Description("自身技能冷却")]
    ActionCooldown,

    [Description("目标正在咏唱技能")]
    ActionCastStart
}
