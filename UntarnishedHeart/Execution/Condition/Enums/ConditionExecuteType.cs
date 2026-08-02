using System.ComponentModel;

namespace UntarnishedHeart.Execution.Condition.Enums;

public enum ConditionExecuteType
{
    [Description("等待 (条件符合前一直等待, 符合后继续)")]
    Wait,

    [Description("跳过 (条件符合才执行, 不符合就跳过)")]
    Skip,

    [Description("重复执行")]
    Repeat
}
