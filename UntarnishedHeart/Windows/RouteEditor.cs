using System.Runtime.CompilerServices;
using Dalamud.Interface.Windowing;
using Newtonsoft.Json;
using OmenTools.OmenService;
using UntarnishedHeart.Execution.Common;
using UntarnishedHeart.Execution.Enums;
using UntarnishedHeart.Execution.Managers;
using UntarnishedHeart.Execution.Preset;
using UntarnishedHeart.Execution.Route;
using UntarnishedHeart.Internal;
using UntarnishedHeart.Windows.Components;

namespace UntarnishedHeart.Windows;

public class RouteEditor() : Window($"路线编辑器###{Plugin.PLUGIN_NAME}-RouteEditor", ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)
{
    private static readonly ConditionalWeakTable<Route, RouteEditorState> EditorStates = [];

    private static int SelectedRouteIndex;

    public override void Draw()
    {
        using var table = ImRaii.Table("RouteEditorTable", 1, ImGuiTableFlags.Resizable);
        if (!table) return;

        ImGui.TableSetupColumn("Content", ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableNextRow();
        ImGui.TableSetColumnIndex(0);

        DrawControlBar();

        ImGui.Separator();

        SelectedRouteIndex = CollectionToolbar.NormalizeSelectedIndex(SelectedRouteIndex, PluginConfig.Instance().Routes.Count);

        if (SelectedRouteIndex < 0)
        {
            ImGui.TextDisabled("请选择一条路线进行编辑");
            return;
        }

        DrawTabContent(PluginConfig.Instance().Routes[SelectedRouteIndex]);
    }

    private static void DrawControlBar()
    {
        CollectionToolbar.DrawSelector
        (
            "选择路线:",
            "###RouteSelectCombo",
            PluginConfig.Instance().Routes,
            ref SelectedRouteIndex,
            route => route.Name,
            route => PluginConfig.Instance().Routes.Remove(route),
            "暂无路线"
        );

        ImGui.SameLine();

        CollectionToolbar.DrawActionButtons
        (
            "SaveRoutes",
            PluginConfig.Instance().Save,
            "AddNewRoute",
            () =>
            {
                PluginConfig.Instance().Routes.Add(new Route { Name = $"新路线 {PluginConfig.Instance().Routes.Count + 1}" });
                SelectedRouteIndex = PluginConfig.Instance().Routes.Count - 1;
            },
            "ImportNewRoute",
            () =>
            {
                var route = ImportRouteFromClipboard();
                if (route == null) return;

                PluginConfig.Instance().Routes.Add(route);
                SelectedRouteIndex = PluginConfig.Instance().Routes.Count - 1;
                PluginConfig.Instance().Save();
            },
            "ExportRoute",
            () =>
            {
                SelectedRouteIndex = CollectionToolbar.NormalizeSelectedIndex(SelectedRouteIndex, PluginConfig.Instance().Routes.Count);
                if (SelectedRouteIndex < 0) return;

                ExportRouteToClipboard(PluginConfig.Instance().Routes[SelectedRouteIndex]);
            },
            PluginConfig.Instance().Routes.Count > 0
        );
    }

    private static void DrawTabContent(Route route)
    {
        var state = EditorStates.GetValue(route, static _ => new RouteEditorState());

        using var tabBar = ImRaii.TabBar("RouteEditorTabs");
        if (!tabBar) return;

        using (var basicInfoTab = ImRaii.TabItem("基本信息"))
        {
            if (basicInfoTab)
                DrawBasicInfoTab(route);
        }

        using (var stepsTab = ImRaii.TabItem("步骤"))
        {
            if (stepsTab)
                DrawStepsTab(route, state);
        }
        
        using (ImRaii.Disabled())
            ImGui.TabItemButton(state.TreeState.CurrentPathTabLabel);
    }

    private static void DrawBasicInfoTab(Route route)
    {
        ImGui.Spacing();

        ImGui.TextColored(KnownColor.LightSkyBlue.ToUInt(), "名称");

        ImGui.SetNextItemWidth(-1f);
        var routeName = route.Name;
        if (ImGui.InputText("###RouteName", ref routeName, 128))
            route.Name = routeName;

        ImGui.TextColored(KnownColor.LightSkyBlue.ToUInt(), "备注");

        var routeRemark = route.Remark;
        if (ImGui.InputTextMultiline("###RouteRemark", ref routeRemark, 4096, new(-1f)))
            route.Remark = routeRemark;
    }

    private static void DrawStepsTab(Route route, RouteEditorState state)
    {
        StepTreeEditor.Draw
        (
            "Route",
            route.Steps,
            state.TreeState,
            state.SharedState,
            GetRunningCursor(route),
            () => new PresetStep { Name = $"步骤 {route.Steps.Count}" }
        );
    }

    private static ExecuteActionRuntimeCursor? GetRunningCursor(Route route)
    {
        if (ExecutionManager.RouteExecutor is not { IsRunning: true } routeExecutor)
            return null;

        if (!ReferenceEquals(routeExecutor.SourceRoute, route))
            return null;

        return routeExecutor.ExecutionCursor.RouteCursor;
    }

    private static Route? ImportRouteFromClipboard()
    {
        try
        {
            var clipboardText = ImGui.GetClipboardText();
            if (string.IsNullOrWhiteSpace(clipboardText)) return null;

            return JsonConvert.DeserializeObject<Route>(clipboardText);
        }
        catch (Exception ex)
        {
            NotifyHelper.Instance().Chat($"导入路线失败: {ex.Message}");
            return null;
        }
    }

    private static void ExportRouteToClipboard(Route route)
    {
        try
        {
            var json = JsonConvert.SerializeObject(route, Formatting.Indented);
            ImGui.SetClipboardText(json);

            NotifyHelper.Instance().Chat("路线已导出到剪贴板");
        }
        catch (Exception ex)
        {
            NotifyHelper.Instance().Chat($"导出路线失败: {ex.Message}");
        }
    }

    private sealed class RouteEditorState
    {
        public StepTreeEditorState   TreeState  { get; }      = new();
        public StepEditorSharedState SharedState { get; }      = new();
    }

    public void Dispose() { }
}
