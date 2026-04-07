using Newtonsoft.Json;
using OmenTools.OmenService;
using UntarnishedHeart.Execution.Condition.Configuration;
using UntarnishedHeart.Execution.Condition.Enums;

namespace UntarnishedHeart.Execution.Condition;

[JsonObject(MemberSerialization.OptIn)]
[ConditionJsonType("HasTarget", ConditionDetectType.HasTarget)]
public sealed class HasTargetCondition : ConditionBase
{
    public override ConditionDetectType Kind => ConditionDetectType.HasTarget;

    [JsonProperty("ComparisonType")]
    public PresenceComparisonType ComparisonType { get; set; } = PresenceComparisonType.Has;

    public override bool Evaluate(in ConditionContext context)
    {
        var hasTarget = TargetManager.Target != null;
        return ComparisonType == PresenceComparisonType.Has ? hasTarget : !hasTarget;
    }

    protected override bool EqualsCore(ConditionBase other) =>
        other is HasTargetCondition condition &&
        ComparisonType == condition.ComparisonType;

    protected override int GetCoreHashCode() => (int)ComparisonType;

    public override ConditionBase DeepCopy() =>
        CopyBasePropertiesTo(new HasTargetCondition { ComparisonType = ComparisonType });

    protected override void DrawBody()
    {
        DrawLabel("比较类型", KnownColor.LightSkyBlue.ToVector4());
        ComparisonType = DrawEnumCombo("###ComparisonTypeCombo", ComparisonType);
    }
}
