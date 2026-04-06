using UntarnishedHeart.Execution.Condition;
using UntarnishedHeart.Execution.ExecuteAction.Enums;
using UntarnishedHeart.Execution.ExecuteAction.Helpers;

namespace UntarnishedHeart.Execution.ExecuteAction.Implementations;

public sealed class RestartCurrentStepAction : ExecuteActionBase
{
    public override ExecuteActionKind Kind => ExecuteActionKind.RestartCurrentStep;

    public override void Draw() => ExecuteActionDrawHelper.DrawNoExtraParametersHint();

    protected override bool EqualsCore(ExecuteActionBase other) => other is RestartCurrentStepAction;

    protected override int GetCoreHashCode() => 0;

    public override ExecuteActionBase DeepCopy() =>
        new RestartCurrentStepAction
        {
            Condition = ConditionCollection.Copy(Condition)
        };
}
