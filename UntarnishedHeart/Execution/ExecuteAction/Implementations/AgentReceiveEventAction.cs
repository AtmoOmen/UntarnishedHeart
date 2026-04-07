using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using Newtonsoft.Json;
using UntarnishedHeart.Execution.Condition;
using UntarnishedHeart.Execution.ExecuteAction.Configuration;
using UntarnishedHeart.Execution.ExecuteAction.Enums;
using UntarnishedHeart.Execution.ExecuteAction.Helpers;
using UntarnishedHeart.Execution.ExecuteAction.Models;

namespace UntarnishedHeart.Execution.ExecuteAction.Implementations;

[JsonObject(MemberSerialization.OptIn)]
[ExecuteActionJsonType("AgentReceiveEvent", ExecuteActionKind.AgentReceiveEvent)]
public sealed class AgentReceiveEventAction : ExecuteActionBase
{
    [JsonProperty("AgentID")]
    public AgentId AgentID { get; set; }

    [JsonProperty("EventKind")]
    public uint EventKind { get; set; }

    [JsonProperty("Parameters")]
    public List<AtkValueParameter> Parameters { get; set; } = [];

    public override ExecuteActionKind Kind => ExecuteActionKind.AgentReceiveEvent;

    public override void Draw()
    {
        ImGui.SetNextItemWidth(240f * GlobalUIScale);
        AgentID = ConditionBase.DrawEnumCombo("代理类型###AgentID", AgentID);

        var eventKind = EventKind;
        ImGui.SetNextItemWidth(240f * GlobalUIScale);
        if (ImGui.InputUInt("事件类型###EventKind", ref eventKind))
            EventKind = eventKind;

        ExecuteActionDrawHelper.DrawAtkValueParameters(Parameters, "AgentReceiveEventParameters");
    }

    protected override bool EqualsCore(ExecuteActionBase other) =>
        other is AgentReceiveEventAction action &&
        AgentID   == action.AgentID             &&
        EventKind == action.EventKind           &&
        Parameters.SequenceEqual(action.Parameters);

    protected override int GetCoreHashCode()
    {
        var hash = new HashCode();
        hash.Add((int)AgentID);
        hash.Add(EventKind);

        foreach (var parameter in Parameters)
            hash.Add(parameter);

        return hash.ToHashCode();
    }

    public override ExecuteActionBase DeepCopy() =>
        CopyBasePropertiesTo
        (
            new AgentReceiveEventAction
            {
                AgentID    = AgentID,
                EventKind  = EventKind,
                Parameters = Parameters.Select(static parameter => parameter.DeepCopy()).ToList()
            }
        );
}
