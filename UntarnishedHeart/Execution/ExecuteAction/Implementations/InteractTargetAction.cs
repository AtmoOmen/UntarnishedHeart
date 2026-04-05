using UntarnishedHeart.Execution.Condition;
using UntarnishedHeart.Execution.ExecuteAction.Enums;
using UntarnishedHeart.Execution.Preset.Helpers;

namespace UntarnishedHeart.Execution.ExecuteAction.Implementations;

public sealed class InteractTargetAction : ExecuteAction
{
    public TargetSelector Selector { get; set; } = new();

    public bool OpenObjectInteraction { get; set; }

    public override ExecuteActionKind Kind => ExecuteActionKind.InteractTarget;

    protected override bool EqualsCore(ExecuteAction other) =>
        other is InteractTargetAction action &&
        Selector.Equals(action.Selector)     &&
        OpenObjectInteraction == action.OpenObjectInteraction;

    protected override int GetCoreHashCode() => HashCode.Combine(Selector, OpenObjectInteraction);

    public override ExecuteAction DeepCopy() =>
        new InteractTargetAction
        {
            Selector              = TargetSelector.Copy(Selector),
            OpenObjectInteraction = OpenObjectInteraction,
            Condition             = ConditionCollection.Copy(Condition)
        };
}
