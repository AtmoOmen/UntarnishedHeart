using System.ComponentModel;

namespace UntarnishedHeart.Execution.Condition.Enums;

public enum ConditionRelationType
{
    [Description("和 (全部条件均需满足)")]
    And,

    [Description("或 (任一条件满足即可)")]
    Or
}
