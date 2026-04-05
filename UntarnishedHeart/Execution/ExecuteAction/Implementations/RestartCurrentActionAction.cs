using UntarnishedHeart.Execution.Condition;
using UntarnishedHeart.Execution.ExecuteAction.Enums;

namespace UntarnishedHeart.Execution.ExecuteAction.Implementations;

public sealed class RestartCurrentActionAction : ExecuteAction
{
    public override ExecuteActionKind Kind => ExecuteActionKind.RestartCurrentAction;

    protected override bool EqualsCore(ExecuteAction other) => other is RestartCurrentActionAction;

    protected override int GetCoreHashCode() => 0;

    public override ExecuteAction DeepCopy() =>
        new RestartCurrentActionAction
        {
            Condition = ConditionCollection.Copy(Condition)
        };
}
