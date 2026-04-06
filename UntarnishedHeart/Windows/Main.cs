using Dalamud.Interface.Windowing;
using Newtonsoft.Json;
using OmenTools.OmenService;
using UntarnishedHeart.Execution.Enums;
using UntarnishedHeart.Execution.Managers;
using UntarnishedHeart.Execution.Preset;
using UntarnishedHeart.Execution.Route;
using UntarnishedHeart.Internal;
using UntarnishedHeart.Windows.Components;

namespace UntarnishedHeart.Windows;

public class Main() : Window($"{Plugin.PLUGIN_NAME} {Plugin.Version}###{Plugin.PLUGIN_NAME}-MainWindow", ImGuiWindowFlags.AlwaysAutoResize)
{
    private static  int SelectedPresetIndex;
    private static  int SelectedRouteIndex;
    internal static int SelectedPresetIndexAccessor => SelectedPresetIndex;
    internal static int SelectedRouteIndexAccessor  => SelectedRouteIndex;

    public override void Draw()
    {
        DrawTopActionRow();

        ImGui.Spacing();
        DrawModeRow();

        ImGui.Separator();
        ImGui.Spacing();

        if (PluginConfig.Instance().CurrentExecutionMode == ExecutionMode.Preset)
            DrawSimpleMode();
        else
            DrawRouteMode();

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        DrawPrimaryActionSection();
    }

    public override void OnClose() => PluginConfig.Instance().Save();

    private static void DrawTopActionRow()
    {
        var width = ImGui.GetContentRegionAvail().X / 4f;

        using var table = ImRaii.Table("MainTopActionRow", 4, ImGuiTableFlags.SizingStretchSame);
        if (!table)
            return;

        ImGui.TableNextRow();

        DrawTopActionButton(0, "预设", width, () => WindowManager.Instance().Get<PresetEditor>().IsOpen ^= true);
        DrawTopActionButton(1, "路线", width, () => WindowManager.Instance().Get<RouteEditor>().IsOpen  ^= true);
        DrawTopActionButton(2, "调试", width, () => WindowManager.Instance().Get<Debug>().IsOpen        ^= true);
        DrawTopActionButton(3, "设置", width, () => ImGui.OpenPopup("MainSettingsPopup"));

        DrawSettingsPopup();
    }

    private static void DrawModeRow()
    {
        var       currentMode = PluginConfig.Instance().CurrentExecutionMode;
        using var table       = ImRaii.Table("MainModeRow", 2, ImGuiTableFlags.SizingStretchSame);
        if (!table)
            return;

        ImGui.TableNextRow();
        ImGui.TableSetColumnIndex(0);

        if (ImGui.RadioButton("预设模式", currentMode == ExecutionMode.Preset))
        {
            PluginConfig.Instance().CurrentExecutionMode = ExecutionMode.Preset;
            PluginConfig.Instance().Save();
            ExecutionManager.StopRouteExecutor();
        }

        ImGui.TableSetColumnIndex(1);

        if (ImGui.RadioButton("路线模式", currentMode == ExecutionMode.Route))
        {
            PluginConfig.Instance().CurrentExecutionMode = ExecutionMode.Route;
            PluginConfig.Instance().Save();
            ExecutionManager.DisposePresetExecutor();
        }
    }

    private static void DrawSettingsPopup()
    {
        using var popup = ImRaii.Popup("MainSettingsPopup");
        if (!popup)
            return;

        ImGui.SetNextItemWidth(180f * GlobalUIScale);
        var changed = ImGui.InputFloat("界面字号###InterfaceFontInput", ref FontManager.Instance().Config.FontSize, 0, 0, "%.1f");
        if (changed)
            FontManager.Instance().Config.FontSize = Math.Clamp(FontManager.Instance().Config.FontSize, 8, 48);

        if (!ImGui.IsItemDeactivatedAfterEdit())
            return;

        FontManager.Instance().Config.Save();
        _ = FontManager.Instance().RebuildUIFontsAsync();
    }

    private static void DrawSimpleMode()
    {
        if (PluginConfig.Instance().Presets.Count == 0)
        {
            DrawEmptyState("暂无预设", () => WindowManager.Instance().Get<PresetEditor>().IsOpen = true, ImportPresetFromClipboard);
            return;
        }

        CollectionToolbar.DrawSelector
        (
            string.Empty,
            "###MainPresetSelectCombo",
            PluginConfig.Instance().Presets,
            ref SelectedPresetIndex,
            preset => preset.Name,
            emptyText: "暂无预设",
            itemWidth: CalculateSelectorWidth(72f)
        );

        ImGui.SameLine();

        if (ImGui.Button("编辑##EditPreset", new(72f * GlobalUIScale, 0f)))
            WindowManager.Instance().Get<PresetEditor>().IsOpen = true;

        ImGui.Spacing();
        DutyOptionsEditor.DrawAndSaveToConfig();
    }

    private static void DrawRouteMode()
    {
        if (PluginConfig.Instance().Routes.Count == 0)
        {
            DrawEmptyState("暂无路线", () => WindowManager.Instance().Get<RouteEditor>().IsOpen = true, ImportRouteFromClipboard);
            return;
        }

        CollectionToolbar.DrawSelector
        (
            string.Empty,
            "###MainRouteSelectCombo",
            PluginConfig.Instance().Routes,
            ref SelectedRouteIndex,
            route => route.Name,
            emptyText: "暂无路线",
            itemWidth: CalculateSelectorWidth(72f)
        );

        ImGui.SameLine();

        if (ImGui.Button("编辑##EditRoute", new(72f * GlobalUIScale, 0f)))
            WindowManager.Instance().Get<RouteEditor>().IsOpen = true;

    }

    private static void DrawPrimaryActionSection()
    {
        var status    = ExecutionUIHelper.CreateStatusViewState();
        var isRunning = status.IsRunning;
        var canStart  = ExecutionUIHelper.CanStartCurrentMode();
        var label     = isRunning ? status.StopLabel : "开始";
        var width     = ImGui.GetContentRegionAvail().X;

        using (ImRaii.Disabled(!isRunning && !canStart))
        {
            if (!ImGui.Button(label, new(width, 0f)))
                return;

            if (isRunning)
            {
                status.StopAction();
                return;
            }

            if (PluginConfig.Instance().CurrentExecutionMode == ExecutionMode.Preset)
                StartSimpleExecution();
            else
                StartRouteExecution();
        }
    }

    private static Preset? GetSelectedPreset()
    {
        SelectedPresetIndex = CollectionToolbar.NormalizeSelectedIndex(SelectedPresetIndex, PluginConfig.Instance().Presets.Count);
        return SelectedPresetIndex >= 0 ? PluginConfig.Instance().Presets[SelectedPresetIndex] : null;
    }

    private static Route? GetSelectedRoute()
    {
        SelectedRouteIndex = CollectionToolbar.NormalizeSelectedIndex(SelectedRouteIndex, PluginConfig.Instance().Routes.Count);
        return SelectedRouteIndex >= 0 ? PluginConfig.Instance().Routes[SelectedRouteIndex] : null;
    }

    private static void DrawEmptyState(string text, Action openEditor, Action importAction)
    {
        ImGui.TextDisabled(text);
        ImGui.Spacing();
        using var table = ImRaii.Table("MainEmptyStateActions", 2, ImGuiTableFlags.SizingStretchSame);
        if (!table)
            return;

        ImGui.TableNextRow();
        ImGui.TableSetColumnIndex(0);
        if (ImGui.Button("编辑", new(0f, 0f)))
            openEditor();

        ImGui.TableSetColumnIndex(1);
        if (ImGui.Button("导入", new(0f, 0f)))
            importAction();
    }

    private static void DrawTopActionButton(int columnIndex, string label, float width, Action onClick)
    {
        ImGui.TableSetColumnIndex(columnIndex);
        if (ImGui.Button(label, new(width - 2 * ImGui.GetStyle().ItemSpacing.X, 1.2f * ImGui.GetTextLineHeightWithSpacing())))
            onClick();
    }

    private static void ImportPresetFromClipboard()
    {
        var preset = Preset.ImportFromClipboard();
        if (preset == null)
            return;

        PluginConfig.Instance().Presets.Add(preset);
        SelectedPresetIndex = PluginConfig.Instance().Presets.Count - 1;
        PluginConfig.Instance().Save();
    }

    private static void ImportRouteFromClipboard()
    {
        try
        {
            var clipboardText = ImGui.GetClipboardText();
            if (string.IsNullOrWhiteSpace(clipboardText))
                return;

            var route = JsonConvert.DeserializeObject<Route>(clipboardText);
            if (route == null)
                return;

            PluginConfig.Instance().Routes.Add(route);
            SelectedRouteIndex = PluginConfig.Instance().Routes.Count - 1;
            PluginConfig.Instance().Save();
        }
        catch (Exception ex)
        {
            NotifyHelper.Instance().Chat($"导入路线失败: {ex.Message}");
        }
    }

    private static void StartSimpleExecution()
    {
        SelectedPresetIndex = CollectionToolbar.NormalizeSelectedIndex(SelectedPresetIndex, PluginConfig.Instance().Presets.Count);
        if (SelectedPresetIndex < 0)
            return;

        ExecutionManager.StartSimpleExecution
        (
            PluginConfig.Instance().Presets[SelectedPresetIndex],
            PluginConfig.Instance().CreatePresetRunOptions()
        );

        ExecutionUIHelper.OpenStatusWindow();
    }

    private static void StartRouteExecution()
    {
        SelectedRouteIndex = CollectionToolbar.NormalizeSelectedIndex(SelectedRouteIndex, PluginConfig.Instance().Routes.Count);
        if (SelectedRouteIndex < 0)
            return;

        ExecutionManager.StartRouteExecution(PluginConfig.Instance().Routes[SelectedRouteIndex]);

        ExecutionUIHelper.OpenStatusWindow();
    }

    private static float CalculateSelectorWidth(float actionButtonWidth)
        => (ImGui.GetContentRegionAvail().X - actionButtonWidth * GlobalUIScale - ImGui.GetStyle().ItemSpacing.X) / GlobalUIScale;
}
