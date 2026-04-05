using System.ComponentModel;

namespace UntarnishedHeart.Execution.Condition.Enums;

public enum ConditionExecuteType
{
    [Description("等待 (不满足条件时, 等待满足, 再继续)")]
    Wait,

    [Description("跳过 (不满足条件时, 直接跳过执行)")]
    Pass,

    [Description("重复 (不满足条件时, 重复执行; 满足时, 仅执行一次)")]
    Repeat
}
