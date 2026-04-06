using UntarnishedHeart.Execution.Condition.Enums;

namespace UntarnishedHeart.Execution.Condition;

public sealed class CompletedDutyCountCondition : RouteValueConditionBase
{
    public override ConditionDetectType Kind => ConditionDetectType.CompletedDutyCount;

    protected override int GetCurrentValue(in ConditionContext context) => context.CompletedDutyCount;

    protected override RouteValueConditionBase DeepCopyCore() =>
        new CompletedDutyCountCondition
        {
            ComparisonType = ComparisonType,
            ExpectedValue  = ExpectedValue
        };
}
