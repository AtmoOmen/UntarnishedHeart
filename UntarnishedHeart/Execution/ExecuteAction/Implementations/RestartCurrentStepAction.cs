using UntarnishedHeart.Execution.Condition;
using UntarnishedHeart.Execution.ExecuteAction.Enums;

namespace UntarnishedHeart.Execution.ExecuteAction.Implementations;

public sealed class RestartCurrentStepAction : ExecuteAction
{
    public override ExecuteActionKind Kind => ExecuteActionKind.RestartCurrentStep;

    protected override bool EqualsCore(ExecuteAction other) => other is RestartCurrentStepAction;

    protected override int GetCoreHashCode() => 0;

    public override ExecuteAction DeepCopy() =>
        new RestartCurrentStepAction
        {
            Condition = ConditionCollection.Copy(Condition)
        };
}
