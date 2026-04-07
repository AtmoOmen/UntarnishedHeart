using Newtonsoft.Json;
using UntarnishedHeart.Execution.ExecuteAction.Configuration;
using UntarnishedHeart.Execution.ExecuteAction.Enums;
using UntarnishedHeart.Execution.ExecuteAction.Helpers;

namespace UntarnishedHeart.Execution.ExecuteAction.Implementations;

[JsonObject(MemberSerialization.OptIn)]
[ExecuteActionJsonType("LeaveDutyAndEndPreset", ExecuteActionKind.LeaveDutyAndEndPreset)]
public sealed class LeaveDutyAndEndAction : ExecuteActionBase
{
    public override ExecuteActionKind Kind =>
        ExecuteActionKind.LeaveDutyAndEndPreset;

    public override void Draw() =>
        ExecuteActionDrawHelper.DrawNoExtraParametersHint();

    protected override bool EqualsCore(ExecuteActionBase other) =>
        other is LeaveDutyAndEndAction;

    protected override int GetCoreHashCode() => 0;

    public override ExecuteActionBase DeepCopy() =>
        CopyBasePropertiesTo(new LeaveDutyAndEndAction());
}
