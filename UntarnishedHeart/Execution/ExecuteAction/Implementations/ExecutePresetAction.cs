using Newtonsoft.Json;
using UntarnishedHeart.Execution.ExecuteAction.Configuration;
using UntarnishedHeart.Execution.ExecuteAction.Enums;
using UntarnishedHeart.Execution.Route;
using UntarnishedHeart.Internal;
using UntarnishedHeart.Windows.Components;

namespace UntarnishedHeart.Execution.ExecuteAction.Implementations;

[JsonObject(MemberSerialization.OptIn)]
[ExecuteActionJsonType("ExecutePreset", ExecuteActionKind.ExecutePreset)]
public sealed class ExecutePresetAction : ExecuteActionBase
{
    [JsonProperty("PresetName")]
    public string PresetName { get; set; } = string.Empty;

    [JsonProperty("DutyOptions")]
    public DutyOptions DutyOptions { get; set; } = new();

    public override ExecuteActionKind Kind => ExecuteActionKind.ExecutePreset;

    public override void Draw()
    {
        var presets             = PluginConfig.Instance().Presets;
        var selectedPresetIndex = -1;

        for (var i = 0; i < presets.Count; i++)
        {
            if (!string.Equals(presets[i].Name, PresetName, StringComparison.Ordinal))
                continue;

            selectedPresetIndex = i;
            break;
        }

        ImGui.AlignTextToFramePadding();
        ImGui.Text("目标预设:");
        ImGui.SameLine();
        ImGui.SetNextItemWidth(240f * GlobalUIScale);

        var preview = selectedPresetIndex >= 0 ? presets[selectedPresetIndex].Name : "暂无预设";
        using (var combo = ImRaii.Combo("###ExecutePresetNameCombo", preview, ImGuiComboFlags.HeightLargest))
        {
            if (combo)
            {
                for (var i = 0; i < presets.Count; i++)
                {
                    if (ImGui.Selectable(presets[i].Name, selectedPresetIndex == i))
                        selectedPresetIndex = i;
                }
            }
        }

        PresetName = selectedPresetIndex >= 0 ? presets[selectedPresetIndex].Name : string.Empty;

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        DutyOptionsEditor.Draw(DutyOptions);
    }

    protected override bool EqualsCore(ExecuteActionBase other) =>
        other is ExecutePresetAction action &&
        PresetName == action.PresetName   &&
        DutyOptions.Equals(action.DutyOptions);

    protected override int GetCoreHashCode() => HashCode.Combine(PresetName, DutyOptions);

    public override ExecuteActionBase DeepCopy() =>
        CopyBasePropertiesTo
        (
            new ExecutePresetAction
            {
                PresetName  = PresetName,
                DutyOptions = UntarnishedHeart.Execution.Route.DutyOptions.Copy(DutyOptions)
            }
        );
}
