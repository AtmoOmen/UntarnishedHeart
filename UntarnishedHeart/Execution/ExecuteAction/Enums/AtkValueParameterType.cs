using System.ComponentModel;

namespace UntarnishedHeart.Execution.ExecuteAction.Enums;

public enum AtkValueParameterType
{
    [Description("整数")]
    Int,

    [Description("非负整数")]
    UInt,

    [Description("浮点数")]
    Float,

    [Description("是/否")]
    Bool,

    [Description("字符串")]
    String
}
