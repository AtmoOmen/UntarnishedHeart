using Newtonsoft.Json;
using UntarnishedHeart.Execution.ExecuteAction.Configuration;
using UntarnishedHeart.Execution.ExecuteAction.Enums;
using UntarnishedHeart.Execution.ExecuteAction.Helpers;

namespace UntarnishedHeart.Execution.ExecuteAction.Implementations;

[JsonObject(MemberSerialization.OptIn)]
[ExecuteActionJsonType("LeaveDutyAndRestartPreset", ExecuteActionKind.LeaveDutyAndRestartPreset)]
public sealed class LeaveDutyAndRestartAction : ExecuteActionBase
{
    public override ExecuteActionKind Kind =>
        ExecuteActionKind.LeaveDutyAndRestartPreset;

    public override void Draw() =>
        ExecuteActionDrawHelper.DrawNoExtraParametersHint();

    protected override bool EqualsCore(ExecuteActionBase other) =>
        other is LeaveDutyAndRestartAction;

    protected override int GetCoreHashCode() => 0;

    public override ExecuteActionBase DeepCopy() =>
        CopyBasePropertiesTo(new LeaveDutyAndRestartAction());
}
