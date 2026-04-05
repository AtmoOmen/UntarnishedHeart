using UntarnishedHeart.Execution.Condition;
using UntarnishedHeart.Execution.ExecuteAction.Enums;

namespace UntarnishedHeart.Execution.ExecuteAction.Implementations;

public sealed class JumpToActionAction : ExecuteAction
{
    public int ActionIndex { get; set; }

    public override ExecuteActionKind Kind => ExecuteActionKind.JumpToAction;

    protected override bool EqualsCore(ExecuteAction other) => other is JumpToActionAction action && ActionIndex == action.ActionIndex;

    protected override int GetCoreHashCode() => ActionIndex;

    public override ExecuteAction DeepCopy() =>
        new JumpToActionAction
        {
            ActionIndex = ActionIndex,
            Condition   = ConditionCollection.Copy(Condition)
        };
}
