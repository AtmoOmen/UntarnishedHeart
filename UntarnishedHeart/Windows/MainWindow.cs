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

public class MainWindow : Window
{
    public MainWindow() : base($"{Plugin.PLUGIN_NAME} {Plugin.Version}###{Plugin.PLUGIN_NAME}-MainWindow")
    {
        SizeConstraints = new()
        {
            MinimumSize = new(300, 400)
        };

        RefreshWindowFlags();
    }

    internal static int SelectedPresetIndexAccessor =>
        CollectionToolbar.NormalizeSelectedIndex(PluginConfig.Instance().SelectedPresetIndex, PluginConfig.Instance().Presets.Count);

    internal static int SelectedRouteIndexAccessor =>
        CollectionToolbar.NormalizeSelectedIndex(PluginConfig.Instance().SelectedRouteIndex, PluginConfig.Instance().Routes.Count);

    public void RefreshWindowFlags() =>
        Flags = PluginConfig.Instance().UnlockMainWindowSize ? ImGuiWindowFlags.None : ImGuiWindowFlags.AlwaysAutoResize;

    public override void Draw()
    {
        NormalizeSelections();
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

        DrawTopActionButton(0, "预设", width, () => WindowManager.Instance().Get<PresetEditor>().IsOpen   ^= true);
        DrawTopActionButton(1, "路线", width, () => WindowManager.Instance().Get<RouteEditor>().IsOpen    ^= true);
        DrawTopActionButton(2, "调试", width, () => WindowManager.Instance().Get<Debug>().IsOpen          ^= true);
        DrawTopActionButton(3, "设置", width, () => WindowManager.Instance().Get<SettingsWindow>().IsOpen ^= true);
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

    private static void DrawSimpleMode()
    {
        var config = PluginConfig.Instance();

        if (config.Presets.Count == 0)
        {
            DrawEmptyState("暂无预设", () => WindowManager.Instance().Get<PresetEditor>().IsOpen = true, ImportPresetFromClipboard);
            return;
        }

        var selectedPresetIndex = config.SelectedPresetIndex;
        CollectionToolbar.DrawSelector
        (
            string.Empty,
            "###MainPresetSelectCombo",
            config.Presets,
            ref selectedPresetIndex,
            preset => preset.Name,
            emptyText: "暂无预设",
            itemWidth: CalculateSelectorWidth(72f)
        );
        PersistSelectedPresetIndex(selectedPresetIndex);

        ImGui.SameLine();
        if (ImGui.Button("编辑##EditPreset", new(72f * GlobalUIScale, 0f)))
            WindowManager.Instance().Get<PresetEditor>().IsOpen = true;

        ImGui.Spacing();
        DutyOptionsEditor.DrawAndSaveToConfig();
    }

    private static void DrawRouteMode()
    {
        var config = PluginConfig.Instance();

        if (config.Routes.Count == 0)
        {
            DrawEmptyState("暂无路线", () => WindowManager.Instance().Get<RouteEditor>().IsOpen = true, ImportRouteFromClipboard);
            return;
        }

        var selectedRouteIndex = config.SelectedRouteIndex;
        CollectionToolbar.DrawSelector
        (
            string.Empty,
            "###MainRouteSelectCombo",
            config.Routes,
            ref selectedRouteIndex,
            route => route.Name,
            emptyText: "暂无路线",
            itemWidth: CalculateSelectorWidth(72f)
        );
        PersistSelectedRouteIndex(selectedRouteIndex);

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
            var clicked = ImGui.Button(label, new(width, 0f));
            if (clicked)
            {
                if (isRunning)
                    status.StopAction();
                else if (PluginConfig.Instance().CurrentExecutionMode == ExecutionMode.Preset)
                    StartSimpleExecution();
                else
                    StartRouteExecution();
            }
        }

        using (ImRaii.Disabled(!status.CanDeferredStop))
        {
            if (ImGui.Button(status.DeferredStopLabel, new(width, 0f)))
                status.DeferredStopAction();
        }
    }

    private static void NormalizeSelections()
    {
        var config                = PluginConfig.Instance();
        var normalizedPresetIndex = CollectionToolbar.NormalizeSelectedIndex(config.SelectedPresetIndex, config.Presets.Count);
        var normalizedRouteIndex  = CollectionToolbar.NormalizeSelectedIndex(config.SelectedRouteIndex,  config.Routes.Count);

        if (config.SelectedPresetIndex == normalizedPresetIndex && config.SelectedRouteIndex == normalizedRouteIndex)
            return;

        config.SelectedPresetIndex = normalizedPresetIndex;
        config.SelectedRouteIndex  = normalizedRouteIndex;
        config.Save();
    }

    private static void PersistSelectedPresetIndex(int selectedPresetIndex)
    {
        var config                = PluginConfig.Instance();
        var normalizedPresetIndex = CollectionToolbar.NormalizeSelectedIndex(selectedPresetIndex, config.Presets.Count);
        if (config.SelectedPresetIndex == normalizedPresetIndex)
            return;

        config.SelectedPresetIndex = normalizedPresetIndex;
        config.Save();
    }

    private static void PersistSelectedRouteIndex(int selectedRouteIndex)
    {
        var config               = PluginConfig.Instance();
        var normalizedRouteIndex = CollectionToolbar.NormalizeSelectedIndex(selectedRouteIndex, config.Routes.Count);
        if (config.SelectedRouteIndex == normalizedRouteIndex)
            return;

        config.SelectedRouteIndex = normalizedRouteIndex;
        config.Save();
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
        var config = PluginConfig.Instance();
        var preset = Preset.ImportFromClipboard();
        if (preset == null)
            return;

        config.Presets.Add(preset);
        config.SelectedPresetIndex = config.Presets.Count - 1;
        config.Save();
    }

    private static void ImportRouteFromClipboard()
    {
        try
        {
            var config        = PluginConfig.Instance();
            var clipboardText = ImGui.GetClipboardText();
            if (string.IsNullOrWhiteSpace(clipboardText))
                return;

            var route = JsonConvert.DeserializeObject<Route>(clipboardText);
            if (route == null)
                return;

            config.Routes.Add(route);
            config.SelectedRouteIndex = config.Routes.Count - 1;
            config.Save();
        }
        catch (Exception ex)
        {
            NotifyHelper.Instance().Chat($"导入路线失败: {ex.Message}");
        }
    }

    private static void StartSimpleExecution()
    {
        var config              = PluginConfig.Instance();
        var selectedPresetIndex = CollectionToolbar.NormalizeSelectedIndex(config.SelectedPresetIndex, config.Presets.Count);
        if (selectedPresetIndex < 0)
            return;

        ExecutionManager.StartSimpleExecution
        (
            config.Presets[selectedPresetIndex],
            config.CreatePresetRunOptions()
        );

        ExecutionUIHelper.OpenStatusWindow();
    }

    private static void StartRouteExecution()
    {
        var config             = PluginConfig.Instance();
        var selectedRouteIndex = CollectionToolbar.NormalizeSelectedIndex(config.SelectedRouteIndex, config.Routes.Count);
        if (selectedRouteIndex < 0)
            return;

        ExecutionManager.StartRouteExecution(config.Routes[selectedRouteIndex]);

        ExecutionUIHelper.OpenStatusWindow();
    }

    private static float CalculateSelectorWidth(float actionButtonWidth)
        => (ImGui.GetContentRegionAvail().X - actionButtonWidth * GlobalUIScale - ImGui.GetStyle().ItemSpacing.X) / GlobalUIScale;
}
