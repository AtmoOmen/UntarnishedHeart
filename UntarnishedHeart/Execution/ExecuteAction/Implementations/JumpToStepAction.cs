using UntarnishedHeart.Execution.Condition;
using UntarnishedHeart.Execution.ExecuteAction.Enums;

namespace UntarnishedHeart.Execution.ExecuteAction.Implementations;

public sealed class JumpToStepAction : ExecuteAction
{
    public int StepIndex { get; set; }

    public override ExecuteActionKind Kind => ExecuteActionKind.JumpToStep;

    protected override bool EqualsCore(ExecuteAction other) => other is JumpToStepAction action && StepIndex == action.StepIndex;

    protected override int GetCoreHashCode() => StepIndex;

    public override ExecuteAction DeepCopy() =>
        new JumpToStepAction
        {
            StepIndex = StepIndex,
            Condition = ConditionCollection.Copy(Condition)
        };
}
