using Dalamud.Game.ClientState.Conditions;
using Dalamud.Interface.Windowing;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using OmenTools.OmenService;
using UntarnishedHeart.Execution.Enums;
using UntarnishedHeart.Execution.Managers;
using UntarnishedHeart.Execution.Preset;
using UntarnishedHeart.Internal;
using UntarnishedHeart.Windows.Components;

namespace UntarnishedHeart.Windows;

public class Main() : Window($"{Plugin.PLUGIN_NAME} {Plugin.Version}###{Plugin.PLUGIN_NAME}-MainWindow")
{
    private static int SelectedPresetIndex;
    private static int SelectedRouteIndex;

    public override void Draw()
    {
        EnsureExamplePresets();

        using var tabBar = ImRaii.TabBar("###MainTabBar");
        if (!tabBar) return;

        using (var mainPageItem = ImRaii.TabItem("主页"))
        {
            if (mainPageItem)
            {
                DrawExecutionModeSelector();

                ImGui.Separator();
                ImGui.Spacing();

                if (PluginConfig.Instance().CurrentExecutionMode == ExecutionMode.Simple)
                    DrawSimpleModeConfig();
                else
                    DrawRouteModeConfig();

                ImGui.Separator();
                ImGui.Spacing();

                DrawExecutionStatus();

                ImGui.Separator();
                ImGui.Spacing();

                DrawExecutionControls();
            }
        }

        if (ImGui.TabItemButton("预设"))
            WindowManager.Instance().Get<PresetEditor>().IsOpen ^= true;

        if (ImGui.TabItemButton("路线"))
            WindowManager.Instance().Get<RouteEditor>().IsOpen ^= true;

        if (ImGui.TabItemButton("调试"))
            WindowManager.Instance().Get<Debug>().IsOpen ^= true;

        using (var othersItem = ImRaii.TabItem("其他"))
        {
            if (othersItem)
            {
                ImGui.TextUnformatted("界面字号");

                using (ImRaii.PushIndent())
                {
                    ImGui.SetNextItemWidth(150f * GlobalUIScale);
                    if (ImGui.InputFloat("###InterfaceFontInput", ref FontManager.Instance().Config.FontSize, 0, 0, "%.1f"))
                        FontManager.Instance().Config.FontSize = Math.Clamp(FontManager.Instance().Config.FontSize, 8, 48);

                    if (ImGui.IsItemDeactivatedAfterEdit())
                    {
                        FontManager.Instance().Config.Save();
                        _ = FontManager.Instance().RebuildUIFontsAsync();
                    }
                }
            }
        }
    }

    public override void OnClose() => PluginConfig.Instance().Save();

    private static void EnsureExamplePresets()
    {
        if (SelectedPresetIndex >= PluginConfig.Instance().Presets.Count || SelectedPresetIndex < 0)
            SelectedPresetIndex = 0;

        if (PluginConfig.Instance().Presets.Count != 0)
            return;

        PluginConfig.Instance().Presets.Add(Preset.ExamplePreset0);
        PluginConfig.Instance().Presets.Add(Preset.ExamplePreset1);
        PluginConfig.Instance().Presets.Add(Preset.ExamplePreset2);
        PluginConfig.Instance().Save();
    }

    private static void DrawExecutionModeSelector()
    {
        ImGui.TextColored(KnownColor.LightSkyBlue.ToVector4(), "执行模式:");
        using var indent = ImRaii.PushIndent();

        var currentMode = PluginConfig.Instance().CurrentExecutionMode;

        if (ImGui.RadioButton("简单模式", currentMode == ExecutionMode.Simple))
        {
            PluginConfig.Instance().CurrentExecutionMode = ExecutionMode.Simple;
            PluginConfig.Instance().Save();

            ExecutionManager.StopRouteExecutor();
        }

        ImGui.SameLine();

        if (ImGui.RadioButton("运行路线", currentMode == ExecutionMode.Route))
        {
            PluginConfig.Instance().CurrentExecutionMode = ExecutionMode.Route;
            PluginConfig.Instance().Save();

            ExecutionManager.DisposePresetExecutor();
        }
    }

    private static void DrawSimpleModeConfig()
    {
        ImGui.TextColored(KnownColor.LightSkyBlue.ToVector4(), "运行设置:");

        using var indent = ImRaii.PushIndent();
        using var group  = ImRaii.Group();

        CollectionToolbar.DrawSelector
        (
            "选择预设:",
            "###MainPresetSelectCombo",
            PluginConfig.Instance().Presets,
            ref SelectedPresetIndex,
            preset => preset.Name,
            preset => PluginConfig.Instance().Presets.Remove(preset),
            "暂无预设"
        );

        ImGui.NewLine();

        var dutyOptions = DutyOptionsEditor.CreateFromConfig();
        if (!DutyOptionsEditor.Draw(dutyOptions))
            return;

        PluginConfig.Instance().LeaderMode           = dutyOptions.LeaderMode;
        PluginConfig.Instance().AutoRecommendGear    = dutyOptions.AutoRecommendGear;
        PluginConfig.Instance().RunTimes             = dutyOptions.RunTimes;
        PluginConfig.Instance().ContentEntryType     = dutyOptions.ContentEntryType;
        PluginConfig.Instance().ContentsFinderOption = dutyOptions.ContentsFinderOption.Clone();
        PluginConfig.Instance().Save();
    }

    private static void DrawRouteModeConfig()
    {
        ImGui.TextColored(KnownColor.LightSkyBlue.ToVector4(), "路线设置:");

        using var indent = ImRaii.PushIndent();
        using var group  = ImRaii.Group();

        if (PluginConfig.Instance().Routes.Count == 0)
        {
            ImGui.Text("暂无路线，请先创建路线");
            return;
        }

        CollectionToolbar.DrawSelector
        (
            "选择路线:",
            "###MainRouteSelectCombo",
            PluginConfig.Instance().Routes,
            ref SelectedRouteIndex,
            route => route.Name,
            route => PluginConfig.Instance().Routes.Remove(route),
            "暂无路线"
        );

        SelectedRouteIndex = CollectionToolbar.NormalizeSelectedIndex(SelectedRouteIndex, PluginConfig.Instance().Routes.Count);
        if (SelectedRouteIndex < 0)
            return;

        var selectedRoute = PluginConfig.Instance().Routes[SelectedRouteIndex];

        if (!string.IsNullOrWhiteSpace(selectedRoute.Note))
            ImGui.Text($"备注: {selectedRoute.Note}");

        ImGui.Text($"步骤数: {selectedRoute.Steps.Count}");
    }

    private static void DrawExecutionStatus()
    {
        if (PluginConfig.Instance().CurrentExecutionMode == ExecutionMode.Simple)
        {
            var presetExecutor = ExecutionManager.PresetExecutor;
            var isRunning      = presetExecutor is { IsDisposed: false } executor && !executor.Completion.IsCompleted;
            var progressText   = $"{presetExecutor?.CurrentRound ?? 0} / {presetExecutor?.MaxRound ?? 0}";
            var message        = presetExecutor?.RunningMessage ?? string.Empty;

            ExecutionControlPanel.DrawStatus("运行信息:", isRunning, "运行次数:", progressText, message);
            return;
        }

        var routeExecutor = ExecutionManager.RouteExecutor;
        var routeRunning  = routeExecutor?.IsRunning == true;
        var routeProgress = $"{routeExecutor?.CurrentStepIndex ?? -1} / {(routeExecutor?.Steps.Count ?? 0) - 1}";
        var routeMessage  = routeExecutor?.RunningMessage ?? string.Empty;

        ExecutionControlPanel.DrawStatus("运行信息:", routeRunning, "当前步骤:", routeProgress, routeMessage);
    }

    private static void DrawExecutionControls()
    {
        if (PluginConfig.Instance().CurrentExecutionMode == ExecutionMode.Simple)
        {
            ExecutionControlPanel.DrawControls
            (
                "开始",
                StartSimpleExecution,
                CanStartSimpleExecution(),
                "结束",
                StopSimpleExecution
            );

            return;
        }

        ExecutionControlPanel.DrawControls
        (
            "开始路线",
            StartRouteExecution,
            CanStartRouteExecution(),
            "停止路线",
            StopRouteExecution
        );
    }

    private static bool CanStartSimpleExecution() =>
        !DService.Instance().Condition.IsBetweenAreas                                                              &&
        (ExecutionManager.PresetExecutor is not { IsDisposed: false } executor || executor.Completion.IsCompleted) &&
        PluginConfig.Instance().Presets.Count > 0;

    private static bool CanStartRouteExecution() =>
        !DService.Instance().Condition.IsBetweenAreas                                                          &&
        ExecutionManager.RouteExecutor is not { IsRunning: true }                                              &&
        PluginConfig.Instance().Routes.Count                                                               > 0 &&
        CollectionToolbar.NormalizeSelectedIndex(SelectedRouteIndex, PluginConfig.Instance().Routes.Count) >= 0;

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
    }

    private static void StopSimpleExecution()
    {
        ExecutionManager.DisposePresetExecutor();

        CancelDutyQueueIfNeeded();
    }

    private static void StartRouteExecution()
    {
        SelectedRouteIndex = CollectionToolbar.NormalizeSelectedIndex(SelectedRouteIndex, PluginConfig.Instance().Routes.Count);
        if (SelectedRouteIndex < 0)
            return;

        ExecutionManager.StartRouteExecution(PluginConfig.Instance().Routes[SelectedRouteIndex]);
    }

    private static void StopRouteExecution()
    {
        ExecutionManager.StopRouteExecutor();

        CancelDutyQueueIfNeeded();
    }

    private static unsafe void CancelDutyQueueIfNeeded()
    {
        if (!DService.Instance().Condition[ConditionFlag.InDutyQueue])
            return;

        AgentId.ContentsFinder.SendEvent(0, 12, 0);
    }
}
