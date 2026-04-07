using Newtonsoft.Json;
using OmenTools.OmenService;
using UntarnishedHeart.Execution.Condition.Configuration;
using UntarnishedHeart.Execution.Condition.Enums;

namespace UntarnishedHeart.Execution.Condition;

[JsonObject(MemberSerialization.OptIn)]
[ConditionJsonType("PlayerLevel", ConditionDetectType.PlayerLevel)]
public sealed class PlayerLevelCondition : RouteValueConditionBase
{
    public override ConditionDetectType Kind => ConditionDetectType.PlayerLevel;

    protected override int GetCurrentValue(in ConditionContext context) => LocalPlayerState.CurrentLevel;

    protected override RouteValueConditionBase DeepCopyCore() =>
        new PlayerLevelCondition
        {
            ComparisonType = ComparisonType,
            ExpectedValue  = ExpectedValue
        };
}
