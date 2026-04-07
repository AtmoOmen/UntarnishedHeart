using Newtonsoft.Json;
using UntarnishedHeart.Execution.ExecuteAction.Configuration;
using UntarnishedHeart.Execution.ExecuteAction.Enums;
using UntarnishedHeart.Execution.ExecuteAction.Helpers;
using UntarnishedHeart.Execution.ExecuteAction.Models;

namespace UntarnishedHeart.Execution.ExecuteAction.Implementations;

[JsonObject(MemberSerialization.OptIn)]
[ExecuteActionJsonType("AddonCallback", ExecuteActionKind.AddonCallback)]
public sealed class AddonCallbackAction : ExecuteActionBase
{
    [JsonProperty("AddonName")]
    public string AddonName { get; set; } = string.Empty;

    [JsonProperty("Parameters")]
    public List<AtkValueParameter> Parameters { get; set; } = [];

    public override ExecuteActionKind Kind => ExecuteActionKind.AddonCallback;

    public override void Draw()
    {
        var addonName = AddonName;
        ImGui.SetNextItemWidth(240f * GlobalUIScale);
        if (ImGui.InputText("界面名称###AddonName", ref addonName, 128))
            AddonName = addonName;

        ExecuteActionDrawHelper.DrawAtkValueParameters(Parameters, "AddonCallbackParameters");
    }

    protected override bool EqualsCore(ExecuteActionBase other) =>
        other is AddonCallbackAction action &&
        AddonName == action.AddonName       &&
        Parameters.SequenceEqual(action.Parameters);

    protected override int GetCoreHashCode()
    {
        var hash = new HashCode();
        hash.Add(AddonName, StringComparer.Ordinal);

        foreach (var parameter in Parameters)
            hash.Add(parameter);

        return hash.ToHashCode();
    }

    public override ExecuteActionBase DeepCopy() =>
        CopyBasePropertiesTo
        (
            new AddonCallbackAction
            {
                AddonName  = AddonName,
                Parameters = Parameters.Select(static parameter => parameter.DeepCopy()).ToList()
            }
        );
}
