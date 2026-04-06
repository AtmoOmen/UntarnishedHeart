using UntarnishedHeart.Execution.Condition;
using UntarnishedHeart.Execution.ExecuteAction.Enums;
using UntarnishedHeart.Execution.ExecuteAction.Helpers;

namespace UntarnishedHeart.Execution.ExecuteAction.Implementations;

public sealed class RestartCurrentActionAction : ExecuteActionBase
{
    public override ExecuteActionKind Kind => ExecuteActionKind.RestartCurrentAction;

    public override void Draw() => ExecuteActionDrawHelper.DrawNoExtraParametersHint();

    protected override bool EqualsCore(ExecuteActionBase other) => other is RestartCurrentActionAction;

    protected override int GetCoreHashCode() => 0;

    public override ExecuteActionBase DeepCopy() =>
        CopyBasePropertiesTo(new RestartCurrentActionAction());
}
