using UntarnishedHeart.Execution.Condition;
using UntarnishedHeart.Execution.ExecuteAction.Enums;

namespace UntarnishedHeart.Execution.ExecuteAction.Implementations;

public sealed class WaitMillisecondsAction : ExecuteAction
{
    public int Milliseconds { get; set; }

    public override ExecuteActionKind Kind => ExecuteActionKind.WaitMilliseconds;

    protected override bool EqualsCore(ExecuteAction other) => other is WaitMillisecondsAction action && Milliseconds == action.Milliseconds;

    protected override int GetCoreHashCode() => Milliseconds;

    public override ExecuteAction DeepCopy() =>
        new WaitMillisecondsAction
        {
            Milliseconds = Milliseconds,
            Condition    = ConditionCollection.Copy(Condition)
        };
}
