using System.ComponentModel;

namespace UntarnishedHeart.Execution.Condition.Legacy;

public enum ConditionComparisonType
{
    [Description("大于")]
    GreaterThan,

    [Description("小于")]
    LessThan,

    [Description("等于")]
    EqualTo,

    [Description("不等于")]
    NotEqualTo,

    [Description("拥有")]
    Has,

    [Description("不拥有")]
    NotHas,

    [Description("完成")]
    Finished,

    [Description("未完成")]
    NotFinished
}
