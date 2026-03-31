using System.ComponentModel;

namespace UntarnishedHeart.Execution.CommandCondition.Enums;

public enum CommandTargetType
{
    [Description("自身")]
    Self,

    [Description("目标")]
    Target
}
