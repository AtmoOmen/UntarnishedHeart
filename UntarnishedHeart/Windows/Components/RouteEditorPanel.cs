using System.Runtime.CompilerServices;
using UntarnishedHeart.Execution.Common;
using UntarnishedHeart.Execution.Managers;
using UntarnishedHeart.Execution.Preset;
using UntarnishedHeart.Execution.Route;

namespace UntarnishedHeart.Windows.Components;

internal static class RouteEditorPanel
{
    private static readonly ConditionalWeakTable<Route, RouteEditorState> EditorStates = [];

    public static void Draw(Route route)
    {
        var state = EditorStates.GetValue(route, static _ => new RouteEditorState());

        using var tabBar = ImRaii.TabBar("###RouteEditor");
        if (!tabBar) return;

        using (var basicInfo = ImRaii.TabItem("基本信息"))
        {
            if (basicInfo)
                DrawBasicInfoTab(route);
        }

        using (var steps = ImRaii.TabItem("步骤"))
        {
            if (steps)
                DrawStepsTab(route, state);
        }

        if (!string.IsNullOrEmpty(state.TreeState.CurrentPathTabLabel))
        {
            ImGui.TabItemButton("###Space");

            using (ImRaii.Disabled())
                ImGui.TabItemButton($"{state.TreeState.CurrentPathTabLabel}###PathLabel");
        }
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

    private sealed class RouteEditorState
    {
        public StepTreeEditorState TreeState { get; } = new();

        public StepEditorSharedState SharedState { get; } = new();
    }
}
