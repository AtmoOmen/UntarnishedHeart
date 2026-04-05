using UntarnishedHeart.Execution.Condition;
using UntarnishedHeart.Execution.ExecuteAction.Enums;

namespace UntarnishedHeart.Execution.ExecuteAction.Implementations;

public sealed class LeaveDutyAndEndAction : ExecuteAction
{
    public override ExecuteActionKind Kind => ExecuteActionKind.LeaveDutyAndEndPreset;

    protected override bool EqualsCore(ExecuteAction other) => other is LeaveDutyAndEndAction;

    protected override int GetCoreHashCode() => 0;

    public override ExecuteAction DeepCopy() =>
        new LeaveDutyAndEndAction
        {
            Condition = ConditionCollection.Copy(Condition)
        };
}
