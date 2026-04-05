using System.ComponentModel;

namespace UntarnishedHeart.Execution.CommandCondition.Enums;

public enum CooldownComparisonType
{
    [Description("完成")]
    Finished,

    [Description("未完成")]
    NotFinished
}
