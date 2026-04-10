using Newtonsoft.Json;
using UntarnishedHeart.Execution.ExecuteAction.Configuration;
using UntarnishedHeart.Execution.ExecuteAction.Enums;
using UntarnishedHeart.Execution.Route;
using UntarnishedHeart.Internal;
using UntarnishedHeart.Windows.Components;
using UntarnishedHeart.Windows;

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

        var preview = selectedPresetIndex >= 0 ? presets[selectedPresetIndex].Name : "暂无预设";
        ImGui.SetNextItemWidth(240f * GlobalUIScale);

        using var combo = ImRaii.Combo("目标预设###ExecutePresetNameCombo", preview, ImGuiComboFlags.HeightLargest);
        if (combo)
            ImGui.CloseCurrentPopup();

        if (ImGui.IsItemClicked())
        {
            var request = new CollectionSelectorRequest
            (
                "选择目标预设",
                "暂无预设",
                selectedPresetIndex,
                presets.Select(preset => new CollectionSelectorItem(preset.Name)).ToArray()
            );

            CollectionSelectorWindow.Open
            (
                request,
                index =>
                {
                    if ((uint)index >= (uint)presets.Count)
                        return;

                    PresetName = presets[index].Name;
                }
            );
        }

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        DutyOptionsEditor.Draw(DutyOptions);
    }

    protected override bool EqualsCore(ExecuteActionBase other) =>
        other is ExecutePresetAction action &&
        PresetName == action.PresetName     &&
        DutyOptions.Equals(action.DutyOptions);

    protected override int GetCoreHashCode() => HashCode.Combine(PresetName, DutyOptions);

    public override ExecuteActionBase DeepCopy() =>
        CopyBasePropertiesTo
        (
            new ExecutePresetAction
            {
                PresetName  = PresetName,
                DutyOptions = DutyOptions.Copy(DutyOptions)
            }
        );
}
