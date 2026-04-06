using UntarnishedHeart.Execution.Condition.Enums;
using UntarnishedHeart.Execution.Condition.Helpers;
using UntarnishedHeart.Execution.Preset.Enums;
using UntarnishedHeart.Execution.Preset.Helpers;

namespace UntarnishedHeart.Execution.Condition;

public sealed class NearbyTargetCondition : ConditionBase
{
    public override ConditionDetectType Kind => ConditionDetectType.NearbyTarget;

    public PresenceComparisonType ComparisonType { get; set; } = PresenceComparisonType.Has;

    public TargetSelector Selector { get; set; } = new() { Kind = TargetSelectorKind.ByObjectKindAndDataID };

    public override bool Evaluate(in ConditionContext context)
    {
        var exists = ResolveSpecificTarget(Selector) != null;
        return ComparisonType == PresenceComparisonType.Has ? exists : !exists;
    }

    protected override bool EqualsCore(ConditionBase other) =>
        other is NearbyTargetCondition condition   &&
        ComparisonType == condition.ComparisonType &&
        Selector.Equals(condition.Selector);

    protected override int GetCoreHashCode() => HashCode.Combine((int)ComparisonType, Selector);

    public override ConditionBase DeepCopy() =>
        CopyBasePropertiesTo
        (
            new NearbyTargetCondition
            {
                ComparisonType = ComparisonType,
                Selector       = TargetSelector.Copy(Selector)
            }
        );

    protected override void DrawBody()
    {
        DrawLabel("比较类型", KnownColor.LightSkyBlue.ToVector4());
        ComparisonType = DrawEnumCombo("###ComparisonTypeCombo", ComparisonType);

        ConditionDrawHelper.DrawTargetSelector(Selector, "Nearby");
    }
}
