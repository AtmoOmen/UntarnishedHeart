using FFXIVClientStructs.FFXIV.Client.Game.UI;
using UntarnishedHeart.Execution.Condition.Enums;

namespace UntarnishedHeart.Execution.Condition;

public sealed class OptimalPartyRecommendationCondition : RouteValueConditionBase
{
    public override ConditionDetectType Kind => ConditionDetectType.OptimalPartyRecommendation;

    protected override unsafe int GetCurrentValue(in ConditionContext context) => PlayerState.Instance()->PlayerCommendations;

    protected override RouteValueConditionBase DeepCopyCore() =>
        new OptimalPartyRecommendationCondition
        {
            ComparisonType = ComparisonType,
            ExpectedValue  = ExpectedValue
        };
}
