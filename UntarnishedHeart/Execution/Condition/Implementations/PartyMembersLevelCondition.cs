using Newtonsoft.Json;
using UntarnishedHeart.Execution.Condition.Configuration;
using UntarnishedHeart.Execution.Condition.Enums;

namespace UntarnishedHeart.Execution.Condition;

[JsonObject(MemberSerialization.OptIn)]
[ConditionJsonType("PartyMembersLevel", ConditionDetectType.PartyMembersLevel)]
public sealed class PartyMembersLevelCondition : RouteValueConditionBase
{
    public override ConditionDetectType Kind => ConditionDetectType.PartyMembersLevel;

    public override bool Evaluate(in ConditionContext context)
    {
        if (DService.Instance().ObjectTable.LocalPlayer is not { } localPlayer)
            return false;

        foreach (var member in DService.Instance().PartyList)
        {
            if (member.EntityId == localPlayer.EntityID)
                continue;

            if (DService.Instance().ObjectTable.SearchByEntityID(member.EntityId) is not ICharacter memberCharacter)
                return false;

            if (!CompareNumeric(ComparisonType, memberCharacter.Level, ExpectedValue))
                return false;
        }

        return true;
    }

    protected override int GetCurrentValue(in ConditionContext context) => 0;

    protected override RouteValueConditionBase DeepCopyCore() =>
        new PartyMembersLevelCondition
        {
            ComparisonType = ComparisonType,
            ExpectedValue  = ExpectedValue
        };
}
