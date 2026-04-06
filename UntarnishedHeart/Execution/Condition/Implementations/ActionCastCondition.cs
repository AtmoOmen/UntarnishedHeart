using UntarnishedHeart.Execution.Condition.Enums;
using UntarnishedHeart.Execution.Condition.Helpers;
using UntarnishedHeart.Execution.Models;

namespace UntarnishedHeart.Execution.Condition;

public sealed class ActionCastCondition : ConditionBase
{
    public override ConditionDetectType Kind => ConditionDetectType.ActionCast;

    public PresenceComparisonType ComparisonType { get; set; } = PresenceComparisonType.Has;

    public ConditionTargetType TargetType { get; set; } = ConditionTargetType.Target;

    public ActionReference Action { get; set; } = new();

    public override bool Evaluate(in ConditionContext context)
    {
        var target = ResolveTarget(context, TargetType);
        var isCasting = target is { IsCasting: true }              &&
                        target.CastActionType == Action.ActionType &&
                        target.CastActionID   == Action.ActionID;

        return ComparisonType == PresenceComparisonType.Has ? isCasting : !isCasting;
    }

    protected override bool EqualsCore(ConditionBase other) =>
        other is ActionCastCondition condition     &&
        ComparisonType == condition.ComparisonType &&
        TargetType     == condition.TargetType     &&
        Action.Equals(condition.Action);

    protected override int GetCoreHashCode() => HashCode.Combine((int)ComparisonType, (int)TargetType, Action);

    public override ConditionBase DeepCopy() =>
        new ActionCastCondition
        {
            ComparisonType = ComparisonType,
            TargetType     = TargetType,
            Action         = ActionReference.Copy(Action)
        };

    protected override void DrawBody()
    {
        DrawLabel("比较类型", KnownColor.LightSkyBlue.ToVector4());
        ComparisonType = DrawEnumCombo("###ComparisonTypeCombo", ComparisonType);

        TargetType = DrawTargetType("###TargetTypeCombo", TargetType);

        ConditionDrawHelper.DrawActionReference(Action);
    }
}
