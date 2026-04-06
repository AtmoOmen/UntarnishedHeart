using System.Numerics;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Windowing;
using Lumina.Excel.Sheets;
using Newtonsoft.Json;
using OmenTools.Interop.Game.Lumina;
using OmenTools.OmenService;
using UntarnishedHeart.Execution.Enums;
using UntarnishedHeart.Execution.Route;
using UntarnishedHeart.Execution.Route.Enums;
using UntarnishedHeart.Internal;
using UntarnishedHeart.Windows.Components;
using UntarnishedHeart.Windows.Helpers;
using Achievement = Lumina.Excel.Sheets.Achievement;

namespace UntarnishedHeart.Windows;

public class RouteEditor() : Window($"路线编辑器###{Plugin.PLUGIN_NAME}-RouteEditor", ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)
{
    private static int        SelectedRouteIndex;
    private static int        DraggedStepIndex = -1;
    private static RouteStep? CopiedStep;
    private        int        selectedStepIndex = -1;

    // Tab状态管理
    private int selectedTabIndex;

    public override void Draw()
    {
        using var table = ImRaii.Table("RouteEditorTable", 1, ImGuiTableFlags.Resizable);
        if (!table) return;

        ImGui.TableSetupColumn("Content", ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableNextRow();
        ImGui.TableSetColumnIndex(0);

        // 控制栏
        DrawControlBar();

        ImGui.Separator();

        // 主体内容 - Tab布局
        if (PluginConfig.Instance().Routes.Count > 0)
        {
            var selectedRoute = PluginConfig.Instance().Routes[SelectedRouteIndex];
            DrawTabContent(selectedRoute);
        }
        else
            ImGui.Text("请选择一个路线进行编辑");
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
                PluginConfig.Instance().Routes.Add(new() { Name = $"新路线 {PluginConfig.Instance().Routes.Count + 1}" });
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

    private void DrawTabContent(Route route)
    {
        using var tabBar = ImRaii.TabBar("RouteEditorTabs");
        if (!tabBar) return;

        // 基本信息Tab
        using (var basicInfoTab = ImRaii.TabItem("基本信息"))
        {
            if (basicInfoTab)
            {
                selectedTabIndex = 0;
                DrawBasicInfoTab(route);
            }
        }

        // 步骤Tab
        using (var stepsTab = ImRaii.TabItem("步骤"))
        {
            if (stepsTab)
            {
                selectedTabIndex = 1;
                DrawStepsTab(route);
            }
        }
    }

    private static void DrawBasicInfoTab(Route route)
    {
        ImGui.Spacing();

        // 路线名称
        ImGui.AlignTextToFramePadding();
        ImGui.Text("名称:");

        ImGui.SameLine();
        ImGui.SetNextItemWidth(-1);
        var routeName = route.Name;
        if (ImGui.InputText("###RouteName", ref routeName, 100))
            route.Name = routeName;

        ImGui.Spacing();

        // 路线备注
        ImGui.AlignTextToFramePadding();
        ImGui.Text("备注:");

        var routeNote = route.Note;
        if (ImGui.InputTextMultiline("###RouteNote", ref routeNote, 1000, new(-1f)))
            route.Note = routeNote;
    }

    private void DrawStepsTab(Route route)
    {
        using var table = ImRaii.Table("StepsTable", 2, ImGuiTableFlags.Resizable | ImGuiTableFlags.BordersInnerV);
        if (!table) return;

        ImGui.TableSetupColumn("StepsList",   ImGuiTableColumnFlags.WidthFixed, 300f * GlobalUIScale);
        ImGui.TableSetupColumn("StepDetails", ImGuiTableColumnFlags.WidthStretch);

        ImGui.TableNextRow();

        ImGui.TableSetColumnIndex(0);
        DrawStepsList(route);

        ImGui.TableSetColumnIndex(1);
        DrawStepDetails(route);
    }

    private void DrawStepsList(Route route)
    {
        if (ImGuiOm.ButtonSelectable("添加步骤"))
        {
            route.Steps.Add(new RouteStep { Name = $"步骤 {route.Steps.Count}" });
            selectedStepIndex = route.Steps.Count - 1; // 选中新添加的步骤
        }

        // 绘制步骤列表
        using var child = ImRaii.Child("StepsListChild", new Vector2(0, 0), true);

        if (child)
        {
            for (var i = 0; i < route.Steps.Count; i++)
                DrawStepListItem(route, i);
        }
    }

    private unsafe void DrawStepListItem(Route route, int index)
    {
        var step     = route.Steps[index];
        var stepName = $"{index}. {step.Name}";

        // 判断是否为选中状态
        var isSelected = selectedStepIndex == index;

        ImGui.PushID(index);

        // 可选择的步骤项
        if (ImGui.Selectable(stepName, isSelected, ImGuiSelectableFlags.AllowDoubleClick))
            selectedStepIndex = index;

        // 开始拖拽
        if (ImGui.BeginDragDropSource(ImGuiDragDropFlags.None))
        {
            ImGui.SetDragDropPayload("STEP_REORDER", BitConverter.GetBytes(index));
            ImGui.Text($"步骤: {stepName}");

            ImGui.EndDragDropSource();
        }

        // 拖拽目标
        if (ImGui.BeginDragDropTarget())
        {
            var payload = ImGui.AcceptDragDropPayload("STEP_REORDER");

            if (!payload.IsNull && payload.Data != null)
            {
                var sourceIndex = *(int*)payload.Data;

                if (sourceIndex != index && sourceIndex >= 0 && sourceIndex < route.Steps.Count)
                {
                    // 执行拖拽排序 - 直接交换两个步骤的位置
                    (route.Steps[sourceIndex], route.Steps[index]) = (route.Steps[index], route.Steps[sourceIndex]);

                    // 更新当前选中步骤
                    if (selectedStepIndex == sourceIndex)
                        selectedStepIndex = index;
                    else if (selectedStepIndex == index)
                        selectedStepIndex = sourceIndex;
                }
            }

            ImGui.EndDragDropTarget();
        }

        // 右键菜单
        using var popup = ImRaii.ContextPopupItem($"StepContextPopup{index}");

        if (popup)
        {
            var contextOperation = StepOperationType.Pass;

            if (ImGui.MenuItem("复制"))
                CopiedStep = RouteStep.Copy(step);

            if (CopiedStep != null)
            {
                using (ImRaii.Group())
                {
                    if (ImGui.MenuItem("粘贴至本步"))
                        contextOperation = StepOperationType.Paste;

                    if (ImGui.MenuItem("向上插入粘贴"))
                        contextOperation = StepOperationType.PasteUp;

                    if (ImGui.MenuItem("向下插入粘贴"))
                        contextOperation = StepOperationType.PasteDown;
                }
            }

            if (ImGui.MenuItem("删除"))
                contextOperation = StepOperationType.Delete;

            ImGui.Separator();

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

            selectedStepIndex = CollectionOperationHelper.Apply
            (
                route.Steps,
                index,
                contextOperation,
                selectedStepIndex,
                () => new RouteStep { Name = $"步骤 {route.Steps.Count}" },
                CopiedStep == null ? null : () => RouteStep.Copy(CopiedStep),
                () => RouteStep.Copy(step)
            );
        }

        ImGui.PopID();
    }

    private void DrawStepDetails(Route route)
    {
        if (selectedStepIndex < 0 || selectedStepIndex >= route.Steps.Count)
        {
            ImGui.TextColored(new Vector4(0.7f, 0.7f, 0.7f, 1.0f), "请选择一个步骤进行编辑");
            return;
        }

        var step = route.Steps[selectedStepIndex];

        DrawStepDetailsContent(step);
    }

    private static void DrawStepDetailsContent(RouteStep step)
    {
        // 基本信息
        ImGui.AlignTextToFramePadding();
        ImGui.Text("名称:");

        ImGui.SameLine();
        ImGui.SetNextItemWidth(-1f);
        var stepName = step.Name;
        if (ImGui.InputText("###StepName", ref stepName, 100))
            step.Name = stepName;

        ImGui.AlignTextToFramePadding();
        ImGui.Text("备注:");

        ImGui.SameLine();
        ImGui.SetNextItemWidth(-1f);
        var stepRemark = step.Remark;
        if (ImGui.InputText("###StepRemark", ref stepRemark, 500))
            step.Remark = stepRemark;

        ImGui.AlignTextToFramePadding();
        ImGui.Text("类型:");

        ImGui.SameLine();
        ImGui.SetNextItemWidth(-1f);

        if (ImGui.BeginCombo("###StepType", step.StepType.GetDescription()))
        {
            foreach (var stepType in Enum.GetValues<RouteStepType>())
            {
                if (ImGui.Selectable(stepType.GetDescription(), step.StepType == stepType))
                    step.StepType = stepType;
            }

            ImGui.EndCombo();
        }

        ImGui.NewLine();

        // 根据步骤类型绘制不同的配置
        switch (step.StepType)
        {
            case RouteStepType.SwitchPreset:
                DrawSwitchPresetConfig(step);
                break;
            case RouteStepType.ConditionCheck:
                DrawConditionCheckConfig(step);
                break;
        }
    }

    private static void DrawSwitchPresetConfig(RouteStep step)
    {
        // 预设选择
        ImGui.SetNextItemWidth(250f * GlobalUIScale);
        using (var combo = ImRaii.Combo("预设###TargetPreset", step.PresetName))
        {
            if (combo)
            {
                foreach (var preset in PluginConfig.Instance().Presets)
                {
                    if (ImGui.Selectable(preset.Name, step.PresetName == preset.Name))
                        step.PresetName = preset.Name;
                }
            }
        }

        // 副本选项配置
        DrawDutyOptions(step.DutyOptions);

        ImGui.NewLine();

        // 预设执行结束后的动作配置
        ImGui.AlignTextToFramePadding();
        ImGui.TextColored(KnownColor.LightSkyBlue.ToUInt(), "预设执行结束后");

        using (ImRaii.PushIndent())
        {
            ImGui.SetNextItemWidth(250f * GlobalUIScale);
            using (var combo = ImRaii.Combo("执行动作###AfterPresetAction", step.AfterPresetAction.GetDescription()))
            {
                if (combo)
                {
                    foreach (var action in Enum.GetValues<RouteStepActionType>())
                    {
                        var actionName = action.GetDescription();
                        if (ImGui.Selectable(actionName, step.AfterPresetAction == action))
                            step.AfterPresetAction = action;
                    }
                }
            }

            // 如果是跳转动作，显示跳转索引输入
            if (step.AfterPresetAction == RouteStepActionType.JumpToStep)
            {
                ImGui.SameLine();
                ImGui.Text("目标步骤:");

                ImGui.SameLine();
                ImGui.SetNextItemWidth(150f * GlobalUIScale);
                var tempJumpIndex = step.AfterPresetJumpIndex;
                if (ImGui.InputInt("###AfterPresetJumpIndex", ref tempJumpIndex))
                    step.AfterPresetJumpIndex = tempJumpIndex;
            }
        }
    }

    private static void DrawConditionCheckConfig(RouteStep step)
    {
        using (var table = ImRaii.Table("ConditionConfigTable", 2, ImGuiTableFlags.SizingFixedFit))
        {
            if (table)
            {
                ImGui.TableSetupColumn("Label",   ImGuiTableColumnFlags.WidthFixed, 100f * GlobalUIScale);
                ImGui.TableSetupColumn("Control", ImGuiTableColumnFlags.WidthStretch);

                // 条件类型
                ImGui.TableNextRow();
                ImGui.TableSetColumnIndex(0);
                ImGui.AlignTextToFramePadding();
                ImGui.Text("条件类型:");

                ImGui.TableSetColumnIndex(1);
                ImGui.SetNextItemWidth(-1f);

                if (ImGui.BeginCombo("###ConditionType", step.ConditionType.GetDescription()))
                {
                    foreach (var conditionType in Enum.GetValues<RouteConditionType>())
                    {
                        var conditionName = conditionType.GetDescription();
                        if (ImGui.Selectable(conditionName, step.ConditionType == conditionType))
                            step.ConditionType = conditionType;
                    }

                    ImGui.EndCombo();
                }

                // 额外ID（成就ID或物品ID）
                if (step.ConditionType is RouteConditionType.AchievementCount or RouteConditionType.ItemCount)
                {
                    ImGui.TableNextRow();
                    ImGui.TableSetColumnIndex(0);
                    ImGui.AlignTextToFramePadding();
                    ImGui.Text(step.ConditionType == RouteConditionType.AchievementCount ? "成就 ID:" : "物品 ID:");

                    ImGui.TableSetColumnIndex(1);
                    ImGui.SetNextItemWidth(200f * GlobalUIScale);
                    var extraID = step.ExtraID;
                    if (ImGui.InputInt("###ExtraId", ref extraID))
                        step.ExtraID = extraID;

                    if (extraID != 0)
                    {
                        switch (step.ConditionType)
                        {
                            case RouteConditionType.AchievementCount when LuminaGetter.TryGetRow((uint)extraID, out Achievement achievementRow):
                                ImGui.SameLine();
                                ImGui.TextUnformatted($"{achievementRow.Name}");
                                ImGuiOm.TooltipHover($"{achievementRow.Description}");
                                break;

                            case RouteConditionType.ItemCount when LuminaGetter.TryGetRow((uint)extraID, out Item itemRow):
                                ImGui.SameLine();
                                ImGui.TextUnformatted($"{itemRow.Name}");
                                ImGuiOm.TooltipHover($"{itemRow.Description}");
                                break;
                        }
                    }
                }

                // 比较类型和条件值
                ImGui.TableNextRow();
                ImGui.TableSetColumnIndex(0);
                ImGui.AlignTextToFramePadding();
                ImGui.Text("比较条件:");

                ImGui.TableSetColumnIndex(1);
                ImGui.SetNextItemWidth(100f * GlobalUIScale);

                if (ImGui.BeginCombo("###ComparisonType", step.ComparisonType.GetDescription()))
                {
                    foreach (var comparisonType in Enum.GetValues<ComparisonType>())
                    {
                        var comparisonName = comparisonType.GetDescription();
                        if (ImGui.Selectable(comparisonName, step.ComparisonType == comparisonType))
                            step.ComparisonType = comparisonType;
                    }

                    ImGui.EndCombo();
                }

                ImGui.SameLine();
                ImGui.SetNextItemWidth(150f * GlobalUIScale);
                var conditionValue = step.ConditionValue;
                if (ImGui.InputInt("###ConditionValue", ref conditionValue))
                    step.ConditionValue = conditionValue;
            }
        }

        ImGui.Spacing();

        // 条件满足时的动作
        ImGui.TextColored(KnownColor.LightGreen.ToVector4(), "条件满足时:");

        var trueAction    = step.TrueAction;
        var trueJumpIndex = step.TrueJumpIndex;
        DrawActionConfig("True", ref trueAction, ref trueJumpIndex);
        step.TrueAction    = trueAction;
        step.TrueJumpIndex = trueJumpIndex;

        ImGui.Spacing();

        // 条件不满足时的动作
        ImGui.TextColored(KnownColor.DarkRed.ToVector4(), "条件不满足时:");

        var falseAction    = step.FalseAction;
        var falseJumpIndex = step.FalseJumpIndex;
        DrawActionConfig("False", ref falseAction, ref falseJumpIndex);
        step.FalseAction    = falseAction;
        step.FalseJumpIndex = falseJumpIndex;
    }

    private static void DrawActionConfig(string prefix, ref RouteStepActionType actionType, ref int jumpIndex)
    {
        using var indent = ImRaii.PushIndent();

        ImGui.AlignTextToFramePadding();
        ImGui.Text("执行动作:");

        ImGui.SameLine();
        ImGui.SetNextItemWidth(150f * GlobalUIScale);

        if (ImGui.BeginCombo($"###ActionType{prefix}", actionType.GetDescription()))
        {
            foreach (var action in Enum.GetValues<RouteStepActionType>())
            {
                var actionName = action.GetDescription();
                if (ImGui.Selectable(actionName, actionType == action))
                    actionType = action;
            }

            ImGui.EndCombo();
        }

        // 如果是跳转动作，显示跳转索引输入
        if (actionType == RouteStepActionType.JumpToStep)
        {
            ImGui.SameLine();
            ImGui.Text("目标步骤:");

            ImGui.SameLine();
            ImGui.SetNextItemWidth(150f * GlobalUIScale);
            var tempJumpIndex = jumpIndex;
            if (ImGui.InputInt($"###JumpIndex{prefix}", ref tempJumpIndex))
                jumpIndex = tempJumpIndex;
        }
    }

    private static void DrawDutyOptions(DutyOptions dutyOptions)
    {
        using var group = ImRaii.Group();
        DutyOptionsEditor.Draw(dutyOptions);
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

    public void Dispose() { }
}
