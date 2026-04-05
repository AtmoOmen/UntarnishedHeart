using OmenTools.OmenService;
using UntarnishedHeart.Execution.Condition.Enums;

namespace UntarnishedHeart.Execution.Condition;

public sealed class HasTargetCondition : Condition
{
    public override ConditionDetectType Kind => ConditionDetectType.HasTarget;

    public PresenceComparisonType ComparisonType { get; set; } = PresenceComparisonType.Has;

    public override bool Evaluate(in ConditionContext context)
    {
        var hasTarget = TargetManager.Target != null;
        return ComparisonType == PresenceComparisonType.Has ? hasTarget : !hasTarget;
    }

    protected override bool EqualsCore(Condition other) =>
        other is HasTargetCondition condition &&
        ComparisonType == condition.ComparisonType;

    protected override int GetCoreHashCode() => (int)ComparisonType;

    public override Condition DeepCopy() =>
        new HasTargetCondition
        {
            ComparisonType = ComparisonType
        };

    protected override void DrawBody()
    {
        DrawLabel("比较类型", KnownColor.LightSkyBlue.ToVector4());
        ComparisonType = DrawEnumCombo("###ComparisonTypeCombo", ComparisonType);
    }
}
