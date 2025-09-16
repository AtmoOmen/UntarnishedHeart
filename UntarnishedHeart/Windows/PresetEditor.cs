using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Utility;
using Dalamud.Interface;
using Dalamud.Interface.Windowing;
using Dalamud.Bindings.ImGui;
using UntarnishedHeart.Executor;
using UntarnishedHeart.Managers;

namespace UntarnishedHeart.Windows;

public class PresetEditor() : Window($"预设编辑器###{PluginName}-PresetEditor")
{
    private static int SelectedPresetIndex;

    public override void Draw()
    {
        ImGui.AlignTextToFramePadding();
        ImGui.Text("选择预设:");

        ImGui.SameLine();
        ImGui.SetNextItemWidth(200f * ImGuiHelpers.GlobalScale);

        if (SelectedPresetIndex > Service.Config.Presets.Count - 1)
            SelectedPresetIndex = 0;

        var selectedPreset = Service.Config.Presets[SelectedPresetIndex];
        using (var combo = ImRaii.Combo("###PresetSelectCombo", $"{selectedPreset.Name}", ImGuiComboFlags.HeightLarge))
        {
            if (combo)
            {
                for (var i = 0; i < Service.Config.Presets.Count; i++)
                {
                    var preset = Service.Config.Presets[i];
                    if (ImGui.Selectable($"{preset.Name}###{preset}-{i}"))
                        SelectedPresetIndex = i;

                    using var popup = ImRaii.ContextPopupItem($"{preset}-{i}ContextPopup");
                    if (popup)
                    {
                        using (ImRaii.Disabled(Service.Config.Presets.Count == 1))
                        {
                            if (ImGui.MenuItem($"删除##{preset}-{i}"))
                                Service.Config.Presets.Remove(preset);
                        }
                    }
                }
            }
        }
        
        ImGui.SameLine();
        if (ImGuiOm.ButtonIcon("SavePresets", FontAwesomeIcon.Save, "保存预设", true))
            Service.Config.Save();

        ImGui.SameLine();
        if (ImGuiOm.ButtonIcon("AddNewPreset", FontAwesomeIcon.FileCirclePlus, "添加预设", true))
        {
            Service.Config.Presets.Add(new());
            SelectedPresetIndex = Service.Config.Presets.Count - 1;
        }

        ImGui.SameLine();
        if (ImGuiOm.ButtonIcon("ImportNewPreset", FontAwesomeIcon.FileImport, "导入预设", true))
        {
            var config = ExecutorPreset.ImportFromClipboard();
            if (config != null)
            {
                Service.Config.Presets.Add(config);
                Service.Config.Save();

                SelectedPresetIndex = Service.Config.Presets.Count - 1;
            }
        }

        ImGui.SameLine();
        if (ImGuiOm.ButtonIcon("ExportPreset", FontAwesomeIcon.FileExport, "导出预设", true))
        {
            var selectedPresetExported = Service.Config.Presets[SelectedPresetIndex];
            selectedPresetExported.ExportToClipboard();
        }

        ImGui.Separator();
        ImGui.Spacing();

        selectedPreset.Draw();
    }

    public void Dispose() { }
}
