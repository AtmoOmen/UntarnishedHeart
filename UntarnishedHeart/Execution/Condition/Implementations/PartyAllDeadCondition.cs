using Newtonsoft.Json;
using UntarnishedHeart.Execution.Condition.Configuration;
using UntarnishedHeart.Execution.Condition.Enums;

namespace UntarnishedHeart.Execution.Condition;

[JsonObject(MemberSerialization.OptIn)]
[ConditionJsonType("PartyAllDead", ConditionDetectType.PartyAllDead)]
public sealed class PartyAllDeadCondition : ConditionBase
{
    public override ConditionDetectType Kind => ConditionDetectType.PartyAllDead;

    [JsonProperty("ComparisonType")]
    public PresenceComparisonType ComparisonType { get; set; } = PresenceComparisonType.Has;

    public override bool Evaluate(in ConditionContext context)
    {
        var partyList = DService.Instance().PartyList;
        if (partyList.Length < 2)
            return ComparisonType == PresenceComparisonType.Has;

        if (DService.Instance().ObjectTable.LocalPlayer is not { } localPlayer)
            return false;

        var allDead = true;

        foreach (var member in partyList)
        {
            if (member.EntityId == localPlayer.EntityID)
                continue;

            if (member.CurrentHP != 0)
            {
                allDead = false;
                break;
            }
        }

        return ComparisonType == PresenceComparisonType.Has ? allDead : !allDead;
    }

    protected override bool EqualsCore(ConditionBase other) =>
        other is PartyAllDeadCondition condition &&
        ComparisonType == condition.ComparisonType;

    protected override int GetCoreHashCode() => (int)ComparisonType;

    public override ConditionBase DeepCopy() =>
        CopyBasePropertiesTo(new PartyAllDeadCondition { ComparisonType = ComparisonType });

    protected override void DrawBody()
    {
        DrawLabel("比较类型", KnownColor.LightSkyBlue.ToVector4());
        ComparisonType = DrawEnumCombo("###ComparisonTypeCombo", ComparisonType);
    }
}
