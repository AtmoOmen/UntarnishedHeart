using UntarnishedHeart.Execution.Condition;
using UntarnishedHeart.Execution.ExecuteAction.Enums;
using UntarnishedHeart.Execution.ExecuteAction.Helpers;

namespace UntarnishedHeart.Execution.ExecuteAction.Implementations;

public sealed class LeaveDutyAndEndAction : ExecuteActionBase
{
    public override ExecuteActionKind Kind => ExecuteActionKind.LeaveDutyAndEndPreset;

    public override void Draw() => ExecuteActionDrawHelper.DrawNoExtraParametersHint();

    protected override bool EqualsCore(ExecuteActionBase other) => other is LeaveDutyAndEndAction;

    protected override int GetCoreHashCode() => 0;

    public override ExecuteActionBase DeepCopy() =>
        new LeaveDutyAndEndAction
        {
            Condition = ConditionCollection.Copy(Condition)
        };
}
