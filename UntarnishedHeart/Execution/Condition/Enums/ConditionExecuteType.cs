using System.ComponentModel;

namespace UntarnishedHeart.Execution.Condition.Enums;

public enum ConditionExecuteType
{
    [Description("等待 (不满足条件时, 等待满足, 再继续)")]
    Wait,

    [Description("跳过 (满足时执行, 不满足时跳过)")]
    Skip,

    [Description("重复 (先判断, 满足前持续重复执行)")]
    Repeat,

    [Description("持续 (先判断, 满足时持续重复执行)")]
    Sustain
}
