using UntarnishedHeart.Execution.Condition;
using UntarnishedHeart.Execution.ExecuteAction.Enums;

namespace UntarnishedHeart.Execution.ExecuteAction.Implementations;

public sealed class InteractNearestObjectAction : ExecuteAction
{
    public override ExecuteActionKind Kind => ExecuteActionKind.InteractNearestObject;

    protected override bool EqualsCore(ExecuteAction other) => other is InteractNearestObjectAction;

    protected override int GetCoreHashCode() => 0;

    public override ExecuteAction DeepCopy() =>
        new InteractNearestObjectAction
        {
            Condition = ConditionCollection.Copy(Condition)
        };
}
