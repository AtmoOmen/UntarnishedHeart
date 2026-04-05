using System.ComponentModel;

namespace UntarnishedHeart.Execution.Condition.Enums;

public enum NumericComparisonType
{
    [Description("大于")]
    GreaterThan,

    [Description("小于")]
    LessThan,

    [Description("等于")]
    EqualTo,

    [Description("不等于")]
    NotEqualTo
}
