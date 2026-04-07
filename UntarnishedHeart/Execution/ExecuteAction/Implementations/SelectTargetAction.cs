using Newtonsoft.Json;
using UntarnishedHeart.Execution.ExecuteAction.Configuration;
using UntarnishedHeart.Execution.ExecuteAction.Enums;
using UntarnishedHeart.Execution.ExecuteAction.Helpers;
using UntarnishedHeart.Execution.Preset.Helpers;

namespace UntarnishedHeart.Execution.ExecuteAction.Implementations;

[JsonObject(MemberSerialization.OptIn)]
[ExecuteActionJsonType("SelectTarget", ExecuteActionKind.SelectTarget)]
public sealed class SelectTargetAction : ExecuteActionBase
{
    [JsonProperty("Selector")]
    public TargetSelector Selector { get; set; } = new();

    public override ExecuteActionKind Kind =>
        ExecuteActionKind.SelectTarget;

    public override void Draw() =>
        ExecuteActionDrawHelper.DrawTargetSelector(Selector, "SelectTarget");

    protected override bool EqualsCore(ExecuteActionBase other) =>
        other is SelectTargetAction action && Selector.Equals(action.Selector);

    protected override int GetCoreHashCode() =>
        Selector.GetHashCode();

    public override ExecuteActionBase DeepCopy() =>
        CopyBasePropertiesTo(new SelectTargetAction { Selector = TargetSelector.Copy(Selector) });
}
