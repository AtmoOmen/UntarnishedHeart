using Newtonsoft.Json;
using OmenTools.ImGuiOm.Widgets.Combos;
using UntarnishedHeart.Execution.Condition;
using UntarnishedHeart.Execution.ExecuteAction.Configuration;
using UntarnishedHeart.Execution.ExecuteAction.Enums;

namespace UntarnishedHeart.Execution.ExecuteAction.Implementations;

[JsonObject(MemberSerialization.OptIn)]
[ExecuteActionJsonType("SwitchClassJob", ExecuteActionKind.SwitchClassJob)]
public sealed class SwitchClassJobAction : ExecuteActionBase
{
    private readonly string jobComboID = $"SwitchClassJob_{Guid.NewGuid():N}";

    [JsonProperty("Mode")]
    public SwitchClassJobMode Mode { get; set; } = SwitchClassJobMode.ByClassJob;

    [JsonProperty("JobId")]
    public uint JobID { get; set; }

    [JsonProperty("GearsetId")]
    public int GearsetID { get; set; }

    public override ExecuteActionKind Kind => ExecuteActionKind.SwitchClassJob;

    public override void Draw()
    {
        ImGui.SetNextItemWidth(240f * GlobalUIScale);
        Mode = ConditionBase.DrawEnumCombo("切换方式###SwitchClassJobModeCombo", Mode);

        switch (Mode)
        {
            case SwitchClassJobMode.ByClassJob:
            {
                var combo = new JobSelectCombo(jobComboID) { SelectedID = JobID };
                if (combo.DrawRadio())
                    JobID = combo.SelectedID;
                break;
            }

            case SwitchClassJobMode.ByGearsetID:
            {
                var gearsetID = GearsetID;
                ImGui.SetNextItemWidth(240f * GlobalUIScale);
                if (ImGui.InputInt("套装编号###SwitchClassJobGearsetIDInput", ref gearsetID))
                    GearsetID = Math.Clamp(gearsetID, 0, 99);
                break;
            }
        }
    }

    protected override bool EqualsCore(ExecuteActionBase other) =>
        other is SwitchClassJobAction action &&
        Mode      == action.Mode             &&
        JobID     == action.JobID            &&
        GearsetID == action.GearsetID;

    protected override int GetCoreHashCode() => HashCode.Combine((int)Mode, JobID, GearsetID);

    public override ExecuteActionBase DeepCopy() =>
        CopyBasePropertiesTo
        (
            new SwitchClassJobAction
            {
                Mode      = Mode,
                JobID     = JobID,
                GearsetID = GearsetID
            }
        );
}
