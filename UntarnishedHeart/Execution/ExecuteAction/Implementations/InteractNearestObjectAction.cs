using UntarnishedHeart.Execution.Condition;
using UntarnishedHeart.Execution.ExecuteAction.Enums;
using UntarnishedHeart.Execution.ExecuteAction.Helpers;

namespace UntarnishedHeart.Execution.ExecuteAction.Implementations;

public sealed class InteractNearestObjectAction : ExecuteActionBase
{
    public override ExecuteActionKind Kind => ExecuteActionKind.InteractNearestObject;

    public override void Draw() => ExecuteActionDrawHelper.DrawNoExtraParametersHint();

    protected override bool EqualsCore(ExecuteActionBase other) => other is InteractNearestObjectAction;

    protected override int GetCoreHashCode() => 0;

    public override ExecuteActionBase DeepCopy() =>
        new InteractNearestObjectAction
        {
            Condition = ConditionCollection.Copy(Condition)
        };
}
