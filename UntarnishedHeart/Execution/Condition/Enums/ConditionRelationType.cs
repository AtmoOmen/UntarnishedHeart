using System.ComponentModel;

namespace UntarnishedHeart.Execution.Condition.Enums;

public enum ConditionRelationType
{
    [Description("全部条件都要符合")]
    And,

    [Description("符合任一条件即可")]
    Or
}
