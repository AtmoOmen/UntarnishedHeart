using OmenTools.OmenService;
using UntarnishedHeart.Execution.Condition.Enums;
using UntarnishedHeart.Execution.Condition.Helpers;
using UntarnishedHeart.Execution.Preset.Enums;
using UntarnishedHeart.Execution.Preset.Helpers;

namespace UntarnishedHeart.Execution.Condition;

public sealed class HasSpecificTargetCondition : ConditionBase
{
    public override ConditionDetectType Kind => ConditionDetectType.HasSpecificTarget;

    public PresenceComparisonType ComparisonType { get; set; } = PresenceComparisonType.Has;

    public TargetSelector Selector { get; set; } = new() { Kind = TargetSelectorKind.ByObjectKindAndDataID };

    public override bool Evaluate(in ConditionContext context)
    {
        var target = TargetManager.Target;
        var matches = target != null &&
                      Selector.Kind switch
                      {
                          TargetSelectorKind.CurrentTarget => true,
                          TargetSelectorKind.ByEntityID    => target.EntityID == Selector.EntityID,
                          TargetSelectorKind.ByObjectKindAndDataID =>
                              target.ObjectKind == Selector.ObjectKind &&
                              target.DataID     == Selector.DataID     &&
                              (!Selector.RequireTargetable || target.IsTargetable),
                          _ => false
                      };

        return ComparisonType == PresenceComparisonType.Has ? matches : !matches;
    }

    protected override bool EqualsCore(ConditionBase other) =>
        other is HasSpecificTargetCondition condition &&
        ComparisonType == condition.ComparisonType    &&
        Selector.Equals(condition.Selector);

    protected override int GetCoreHashCode() => HashCode.Combine((int)ComparisonType, Selector);

    public override ConditionBase DeepCopy() =>
        new HasSpecificTargetCondition
        {
            ComparisonType = ComparisonType,
            Selector       = TargetSelector.Copy(Selector)
        };

    protected override void DrawBody()
    {
        DrawLabel("比较类型", KnownColor.LightSkyBlue.ToVector4());
        ComparisonType = DrawEnumCombo("###ComparisonTypeCombo", ComparisonType);

        ConditionDrawHelper.DrawTargetSelector(Selector, "HasSpecificTarget");
    }
}
