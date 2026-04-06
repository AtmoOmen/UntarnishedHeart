using OmenTools.OmenService;
using UntarnishedHeart.Execution.Condition.Enums;
using UntarnishedHeart.Execution.Condition.Helpers;
using UntarnishedHeart.Execution.Models;

namespace UntarnishedHeart.Execution.Condition;

public sealed class ActionCooldownCondition : ConditionBase
{
    public override ConditionDetectType Kind => ConditionDetectType.ActionCooldown;

    public CooldownComparisonType ComparisonType { get; set; } = CooldownComparisonType.Finished;

    public ActionReference Action { get; set; } = new();

    public override bool Evaluate(in ConditionContext context)
    {
        var isOffCooldown = UseActionManager.Instance().IsActionOffCooldown(Action.ActionType, Action.ActionID);
        return ComparisonType switch
        {
            CooldownComparisonType.Finished    => isOffCooldown,
            CooldownComparisonType.NotFinished => !isOffCooldown,
            _                                  => false
        };
    }

    protected override bool EqualsCore(ConditionBase other) =>
        other is ActionCooldownCondition condition &&
        ComparisonType == condition.ComparisonType &&
        Action.Equals(condition.Action);

    protected override int GetCoreHashCode() => HashCode.Combine((int)ComparisonType, Action);

    public override ConditionBase DeepCopy() =>
        new ActionCooldownCondition
        {
            ComparisonType = ComparisonType,
            Action         = ActionReference.Copy(Action)
        };

    protected override void DrawBody()
    {
        DrawLabel("比较类型", KnownColor.LightSkyBlue.ToVector4());
        ComparisonType = DrawEnumCombo("###ComparisonTypeCombo", ComparisonType);

        ConditionDrawHelper.DrawActionReference(Action);
    }
}
