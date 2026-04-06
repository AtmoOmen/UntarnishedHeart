using UntarnishedHeart.Execution.Condition.Enums;

namespace UntarnishedHeart.Execution.Condition;

public sealed class TargetTargetIsSelfCondition : ConditionBase
{
    public override ConditionDetectType Kind => ConditionDetectType.TargetTargetIsSelf;

    public PresenceComparisonType ComparisonType { get; set; } = PresenceComparisonType.Has;

    public override bool Evaluate(in ConditionContext context)
    {
        var isSelf = context.Target                != null &&
                     context.LocalPlayer           != null &&
                     context.Target.TargetObjectID == context.LocalPlayer.GameObjectID;

        return ComparisonType == PresenceComparisonType.Has ? isSelf : !isSelf;
    }

    protected override bool EqualsCore(ConditionBase other) =>
        other is TargetTargetIsSelfCondition condition &&
        ComparisonType == condition.ComparisonType;

    protected override int GetCoreHashCode() => (int)ComparisonType;

    public override ConditionBase DeepCopy() =>
        CopyBasePropertiesTo(new TargetTargetIsSelfCondition { ComparisonType = ComparisonType });

    protected override void DrawBody()
    {
        DrawLabel("比较类型", KnownColor.LightSkyBlue.ToVector4());
        ComparisonType = DrawEnumCombo("###ComparisonTypeCombo", ComparisonType);
    }
}
