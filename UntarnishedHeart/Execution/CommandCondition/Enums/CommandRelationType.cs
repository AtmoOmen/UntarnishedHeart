using System.ComponentModel;

namespace UntarnishedHeart.Execution.CommandCondition.Enums;

public enum CommandRelationType
{
    [Description("和 (全部条件均需满足)")]
    And,

    [Description("或 (任一条件满足即可)")]
    Or
}
