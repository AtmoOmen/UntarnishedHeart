using System.Numerics;
using System.Runtime.CompilerServices;
using Dalamud.Interface.Windowing;
using Newtonsoft.Json;
using OmenTools.OmenService;
using UntarnishedHeart.Execution.Enums;
using UntarnishedHeart.Execution.Preset;
using UntarnishedHeart.Execution.Route;
using UntarnishedHeart.Internal;
using UntarnishedHeart.Windows.Components;
using UntarnishedHeart.Windows.Helpers;

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
            ImGui.Text("请选择一个路线进行编辑");
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
    }

    private static void DrawBasicInfoTab(Route route)
    {
        ImGui.Spacing();

        ImGui.AlignTextToFramePadding();
        ImGui.Text("名称:");
        ImGui.SameLine();
        ImGui.SetNextItemWidth(-1f);

        var routeName = route.Name;
        if (ImGui.InputText("###RouteName", ref routeName, 100))
            route.Name = routeName;

        ImGui.Spacing();

        ImGui.AlignTextToFramePadding();
        ImGui.Text("备注:");

        var routeRemark = route.Remark;
        if (ImGui.InputTextMultiline("###RouteRemark", ref routeRemark, 2000, new(-1f)))
            route.Remark = routeRemark;
    }

    private static unsafe void DrawStepsTab(Route route, RouteEditorState state)
    {
        state.CurrentStep = NormalizeCurrentStep(state.CurrentStep, route.Steps.Count);

        using var table = ImRaii.Table("RouteStepsTable", 2, ImGuiTableFlags.Resizable | ImGuiTableFlags.BordersInnerV);
        if (!table) return;

        ImGui.TableSetupColumn("StepsList",   ImGuiTableColumnFlags.WidthFixed, 200f * GlobalUIScale);
        ImGui.TableSetupColumn("StepDetails", ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableNextRow();

        ImGui.TableSetColumnIndex(0);
        if (ImGuiOm.ButtonStretch("添加步骤"))
            route.Steps.Add(new PresetStep { Name = $"步骤 {route.Steps.Count}" });

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        using (var child = ImRaii.Child("RouteStepsSelectChild", ImGui.GetContentRegionAvail(), true))
        {
            if (child)
            {
                for (var i = 0; i < route.Steps.Count; i++)
                {
                    var step        = route.Steps[i];
                    var actionCount = step.EnterActions.Count + step.BodyActions.Count + step.ExitActions.Count;
                    var stepName    = $"{i}. {step.Name} ({actionCount} 个动作)";

                    if (ImGui.Selectable(stepName, i == state.CurrentStep, ImGuiSelectableFlags.AllowDoubleClick))
                        state.CurrentStep = i;

                    if (ImGui.BeginDragDropSource(ImGuiDragDropFlags.None))
                    {
                        ImGui.SetDragDropPayload("STEP_REORDER", BitConverter.GetBytes(i));
                        ImGui.Text($"步骤: {stepName}");
                        ImGui.EndDragDropSource();
                    }

                    if (ImGui.BeginDragDropTarget())
                    {
                        var payload = ImGui.AcceptDragDropPayload("STEP_REORDER");

                        if (!payload.IsNull && payload.Data != null)
                        {
                            var sourceIndex = *(int*)payload.Data;

                            if (sourceIndex != i && sourceIndex >= 0 && sourceIndex < route.Steps.Count)
                            {
                                (route.Steps[sourceIndex], route.Steps[i]) = (route.Steps[i], route.Steps[sourceIndex]);

                                if (state.CurrentStep == sourceIndex)
                                    state.CurrentStep = i;
                                else if (state.CurrentStep == i)
                                    state.CurrentStep = sourceIndex;
                            }
                        }

                        ImGui.EndDragDropTarget();
                    }

                    DrawStepContextMenu(route, state, i, step);
                }
            }
        }

        ImGui.TableSetColumnIndex(1);

        using var detailsChild = ImRaii.Child("RouteStepsDrawChild", ImGui.GetContentRegionAvail(), true, ImGuiWindowFlags.NoBackground);
        if (!detailsChild) return;

        if (state.CurrentStep < 0 || state.CurrentStep >= route.Steps.Count)
        {
            ImGui.TextColored(new Vector4(0.7f, 0.7f, 0.7f, 1f), "请选择一个步骤进行编辑");
            return;
        }

        var currentStep      = route.Steps[state.CurrentStep];
        var currentStepIndex = state.CurrentStep;
        PresetStepEditor.Draw(currentStep, ref currentStepIndex, route.Steps, state.SharedState);
        state.CurrentStep = currentStepIndex;
    }

    private static void DrawStepContextMenu(Route route, RouteEditorState state, int index, PresetStep step)
    {
        var contextOperation = StepOperationType.Pass;

        if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
            ImGui.OpenPopup($"RouteStepContentMenu_{index}");

        using var context = ImRaii.ContextPopupItem($"RouteStepContentMenu_{index}");
        if (!context) return;

        ImGui.Text($"第 {index} 步: {step.Name}");
        ImGui.Separator();

        if (ImGui.MenuItem("复制"))
            state.SharedState.StepToCopy = PresetStep.Copy(step);

        if (state.SharedState.StepToCopy != null)
        {
            if (ImGui.MenuItem("粘贴至本步"))
                contextOperation = StepOperationType.Paste;

            if (ImGui.MenuItem("向上插入粘贴"))
                contextOperation = StepOperationType.PasteUp;

            if (ImGui.MenuItem("向下插入粘贴"))
                contextOperation = StepOperationType.PasteDown;
        }

        if (ImGui.MenuItem("删除"))
            contextOperation = StepOperationType.Delete;

        if (index > 0 && ImGui.MenuItem("上移"))
            contextOperation = StepOperationType.MoveUp;

        if (index < route.Steps.Count - 1 && ImGui.MenuItem("下移"))
            contextOperation = StepOperationType.MoveDown;

        ImGui.Separator();

        if (ImGui.MenuItem("向上插入新步骤"))
            contextOperation = StepOperationType.InsertUp;

        if (ImGui.MenuItem("向下插入新步骤"))
            contextOperation = StepOperationType.InsertDown;

        ImGui.Separator();

        if (ImGui.MenuItem("复制并插入本步骤"))
            contextOperation = StepOperationType.PasteCurrent;

        state.CurrentStep = CollectionOperationHelper.Apply
        (
            route.Steps,
            index,
            contextOperation,
            state.CurrentStep,
            () => new PresetStep { Name = $"步骤 {route.Steps.Count}" },
            state.SharedState.StepToCopy == null ? null : () => PresetStep.Copy(state.SharedState.StepToCopy),
            () => PresetStep.Copy(step)
        );
    }

    private static int NormalizeCurrentStep(int currentStep, int count)
    {
        if (count == 0)
            return -1;

        return Math.Clamp(currentStep, 0, count - 1);
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
        public int                   CurrentStep { get; set; } = -1;
        public StepEditorSharedState SharedState { get; } = new();
    }

    public void Dispose() { }
}
