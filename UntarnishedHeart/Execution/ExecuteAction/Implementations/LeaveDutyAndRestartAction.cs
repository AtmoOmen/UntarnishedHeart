using UntarnishedHeart.Execution.Condition;
using UntarnishedHeart.Execution.ExecuteAction.Enums;

namespace UntarnishedHeart.Execution.ExecuteAction.Implementations;

public sealed class LeaveDutyAndRestartAction : ExecuteAction
{
    public override ExecuteActionKind Kind => ExecuteActionKind.LeaveDutyAndRestartPreset;

    protected override bool EqualsCore(ExecuteAction other) => other is LeaveDutyAndRestartAction;

    protected override int GetCoreHashCode() => 0;

    public override ExecuteAction DeepCopy() =>
        new LeaveDutyAndRestartAction
        {
            Condition = ConditionCollection.Copy(Condition)
        };
}
