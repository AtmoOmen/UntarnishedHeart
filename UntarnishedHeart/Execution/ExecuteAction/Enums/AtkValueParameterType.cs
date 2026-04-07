using System.ComponentModel;

namespace UntarnishedHeart.Execution.ExecuteAction.Enums;

public enum AtkValueParameterType
{
    [Description("整数")]
    Int,

    [Description("无符号整数")]
    UInt,

    [Description("浮点数")]
    Float,

    [Description("布尔")]
    Bool,

    [Description("字符串")]
    String
}
