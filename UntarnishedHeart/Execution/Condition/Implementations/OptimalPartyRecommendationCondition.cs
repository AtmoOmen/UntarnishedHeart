using FFXIVClientStructs.FFXIV.Client.Game.UI;
using Newtonsoft.Json;
using UntarnishedHeart.Execution.Condition.Configuration;
using UntarnishedHeart.Execution.Condition.Enums;

namespace UntarnishedHeart.Execution.Condition;

[JsonObject(MemberSerialization.OptIn)]
[ConditionJsonType("OptimalPartyRecommendation", ConditionDetectType.OptimalPartyRecommendation)]
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
