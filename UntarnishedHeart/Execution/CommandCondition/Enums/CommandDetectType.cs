using System.ComponentModel;

namespace UntarnishedHeart.Execution.CommandCondition.Enums;

public enum CommandDetectType
{
    [Description("生命值百分比 (大于/小于/等于/不等于)")]
    Health,

    [Description("状态效果 (拥有/不拥有)")]
    Status,

    [Description("技能冷却 [自身] (完成/未完成)")]
    ActionCooldown,

    [Description("技能咏唱开始 [目标] (拥有)")]
    ActionCastStart
}
