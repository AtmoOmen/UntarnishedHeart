using UntarnishedHeart.Execution.Condition;
using UntarnishedHeart.Execution.ExecuteAction.Enums;
using UntarnishedHeart.Execution.Preset.Helpers;

namespace UntarnishedHeart.Execution.ExecuteAction.Implementations;

public sealed class SelectTargetAction : ExecuteAction
{
    public TargetSelector Selector { get; set; } = new();

    public override ExecuteActionKind Kind => ExecuteActionKind.SelectTarget;

    protected override bool EqualsCore(ExecuteAction other) => other is SelectTargetAction action && Selector.Equals(action.Selector);

    protected override int GetCoreHashCode() => Selector.GetHashCode();

    public override ExecuteAction DeepCopy() =>
        new SelectTargetAction
        {
            Selector  = TargetSelector.Copy(Selector),
            Condition = ConditionCollection.Copy(Condition)
        };
}
