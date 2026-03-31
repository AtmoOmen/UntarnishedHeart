using Dalamud.Interface.Windowing;
using UntarnishedHeart.Execution.Preset;
using UntarnishedHeart.Internal;
using UntarnishedHeart.Windows.Components;

namespace UntarnishedHeart.Windows;

public class PresetEditor() : Window($"预设编辑器###{Plugin.PLUGIN_NAME}-PresetEditor", ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)
{
    private static int SelectedPresetIndex;

    public override void Draw()
    {
        CollectionToolbar.DrawSelector
        (
            "选择预设:",
            "###PresetSelectCombo",
            PluginConfig.Instance().Presets,
            ref SelectedPresetIndex,
            preset => preset.Name,
            preset => PluginConfig.Instance().Presets.Remove(preset),
            "暂无预设"
        );

        ImGui.SameLine();

        CollectionToolbar.DrawActionButtons
        (
            "SavePresets",
            PluginConfig.Instance().Save,
            "AddNewPreset",
            () =>
            {
                PluginConfig.Instance().Presets.Add(new());
                SelectedPresetIndex = PluginConfig.Instance().Presets.Count - 1;
            },
            "ImportNewPreset",
            () =>
            {
                var config = Preset.ImportFromClipboard();
                if (config == null) return;

                PluginConfig.Instance().Presets.Add(config);
                SelectedPresetIndex = PluginConfig.Instance().Presets.Count - 1;
                PluginConfig.Instance().Save();
            },
            "ExportPreset",
            () =>
            {
                SelectedPresetIndex = CollectionToolbar.NormalizeSelectedIndex(SelectedPresetIndex, PluginConfig.Instance().Presets.Count);
                if (SelectedPresetIndex < 0) return;

                PluginConfig.Instance().Presets[SelectedPresetIndex].ExportToClipboard();
            },
            PluginConfig.Instance().Presets.Count > 0
        );

        ImGui.Separator();
        ImGui.Spacing();

        SelectedPresetIndex = CollectionToolbar.NormalizeSelectedIndex(SelectedPresetIndex, PluginConfig.Instance().Presets.Count);

        if (SelectedPresetIndex < 0)
        {
            ImGui.Text("暂无预设，请先添加或导入");
            return;
        }

        PresetEditorPanel.Draw(PluginConfig.Instance().Presets[SelectedPresetIndex]);
    }

    public void Dispose() { }
}
