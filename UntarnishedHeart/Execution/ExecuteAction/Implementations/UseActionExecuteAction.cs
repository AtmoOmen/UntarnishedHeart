using System.Numerics;
using Newtonsoft.Json;
using UntarnishedHeart.Execution.ExecuteAction.Configuration;
using UntarnishedHeart.Execution.ExecuteAction.Enums;
using UntarnishedHeart.Execution.ExecuteAction.Helpers;
using UntarnishedHeart.Execution.Models;
using UntarnishedHeart.Execution.Preset.Helpers;

namespace UntarnishedHeart.Execution.ExecuteAction.Implementations;

[JsonObject(MemberSerialization.OptIn)]
[ExecuteActionJsonType("UseAction", ExecuteActionKind.UseAction)]
public sealed class UseActionExecuteAction : ExecuteActionBase
{
    [JsonProperty("Action")]
    public ActionReference Action { get; set; } = new();

    [JsonProperty("TargetSelector")]
    public TargetSelector TargetSelector { get; set; } = new();

    [JsonProperty("Location")]
    public Vector3 Location { get; set; }

    [JsonProperty("UseLocation")]
    public bool UseLocation { get; set; }

    public override ExecuteActionKind Kind => ExecuteActionKind.UseAction;

    public override void Draw()
    {
        ExecuteActionDrawHelper.DrawActionReference(Action, "UseAction");

        using (ImRaii.Disabled(UseLocation))
            ExecuteActionDrawHelper.DrawTargetSelector(TargetSelector, "UseActionTarget");

        var useLocation = UseLocation;
        if (ImGui.Checkbox("按地面坐标释放###UseLocation", ref useLocation))
            UseLocation = useLocation;

        if (!UseLocation)
            return;

        var location = Location;
        if (ImGui.InputFloat3("地面坐标###UseActionLocation", ref location))
            Location = location;

        ExecuteActionDrawHelper.DrawPositionSelector("UseActionGetCurrentPosition", position => Location = position);
    }

    protected override bool EqualsCore(ExecuteActionBase other) =>
        other is UseActionExecuteAction action       &&
        Action.Equals(action.Action)                 &&
        TargetSelector.Equals(action.TargetSelector) &&
        Location.Equals(action.Location)             &&
        UseLocation == action.UseLocation;

    protected override int GetCoreHashCode() => HashCode.Combine(Action, TargetSelector, Location, UseLocation);

    public override ExecuteActionBase DeepCopy() =>
        CopyBasePropertiesTo
        (
            new UseActionExecuteAction
            {
                Action         = ActionReference.Copy(Action),
                TargetSelector = TargetSelector.Copy(TargetSelector),
                Location       = Location,
                UseLocation    = UseLocation
            }
        );
}
