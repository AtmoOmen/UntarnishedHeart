using System;
using System.Numerics;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Windowing;
using Lumina.Excel.Sheets;
using Newtonsoft.Json;
using UntarnishedHeart.Executor;
using UntarnishedHeart.Managers;
using Achievement = Lumina.Excel.Sheets.Achievement;

namespace UntarnishedHeart.Windows;

public class RouteEditor() : Window($"路线编辑器###{PluginName}-RouteEditor", ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)
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
        if (Service.Config.Routes.Count > 0)
        {
            var selectedRoute = Service.Config.Routes[SelectedRouteIndex];
            DrawTabContent(selectedRoute);
        }
        else
            ImGui.Text("请选择一个路线进行编辑");
    }

    private static void DrawControlBar()
    {
        ImGui.AlignTextToFramePadding();
        ImGui.Text("选择路线:");

        ImGui.SameLine();
        ImGui.SetNextItemWidth(200f * ImGuiHelpers.GlobalScale);

        if (SelectedRouteIndex > Service.Config.Routes.Count - 1)
            SelectedRouteIndex = 0;

        if (Service.Config.Routes.Count == 0)
            ImGui.Text("暂无路线");
        else
        {
            var selectedRoute = Service.Config.Routes[SelectedRouteIndex];

            using (var combo = ImRaii.Combo("###RouteSelectCombo", $"{selectedRoute.Name}", ImGuiComboFlags.HeightLarge))
            {
                if (combo)
                {
                    for (var i = 0; i < Service.Config.Routes.Count; i++)
                    {
                        var route = Service.Config.Routes[i];
                        if (ImGui.Selectable($"{route.Name}###{route}-{i}"))
                            SelectedRouteIndex = i;

                        using var popup = ImRaii.ContextPopupItem($"{route}-{i}ContextPopup");

                        if (popup)
                        {
                            using (ImRaii.Disabled(Service.Config.Routes.Count == 1))
                            {
                                if (ImGui.MenuItem($"删除##{route}-{i}"))
                                    Service.Config.Routes.Remove(route);
                            }
                        }
                    }
                }
            }
        }

        ImGui.SameLine();
        if (ImGuiOm.ButtonIcon("SaveRoutes", FontAwesomeIcon.Save, "保存路线", true))
            Service.Config.Save();

        ImGui.SameLine();

        if (ImGuiOm.ButtonIcon("AddNewRoute", FontAwesomeIcon.FileCirclePlus, "添加路线", true))
        {
            Service.Config.Routes.Add(new() { Name = $"新路线 {Service.Config.Routes.Count + 1}" });
            SelectedRouteIndex = Service.Config.Routes.Count - 1;
        }

        ImGui.SameLine();

        if (ImGuiOm.ButtonIcon("ImportNewRoute", FontAwesomeIcon.FileImport, "导入路线", true))
        {
            var route = ImportRouteFromClipboard();

            if (route != null)
            {
                Service.Config.Routes.Add(route);
                Service.Config.Save();

                SelectedRouteIndex = Service.Config.Routes.Count - 1;
            }
        }

        ImGui.SameLine();

        if (ImGuiOm.ButtonIcon("ExportRoute", FontAwesomeIcon.FileExport, "导出路线", true) && Service.Config.Routes.Count > 0)
        {
            var selectedRouteExported = Service.Config.Routes[SelectedRouteIndex];
            ExportRouteToClipboard(selectedRouteExported);
        }
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

        ImGui.TableSetupColumn("StepsList",   ImGuiTableColumnFlags.WidthFixed, 300f * ImGuiHelpers.GlobalScale);
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
            if (ImGui.MenuItem("复制"))
                CopiedStep = RouteStep.Copy(step);

            using (ImRaii.Disabled(CopiedStep == null))
            {
                if (ImGui.MenuItem("粘贴") && CopiedStep != null)
                {
                    route.Steps.Insert(index + 1, RouteStep.Copy(CopiedStep));
                    selectedStepIndex = index + 1; // 选中新粘贴的步骤
                }
            }

            if (ImGui.MenuItem("删除"))
            {
                route.Steps.RemoveAt(index);
                // 调整选中索引
                if (selectedStepIndex >= route.Steps.Count)
                    selectedStepIndex = route.Steps.Count - 1;
                if (selectedStepIndex < 0 && route.Steps.Count > 0)
                    selectedStepIndex = 0;
                ImGui.PopID();
                return;
            }

            ImGui.Separator();

            if (ImGui.MenuItem("上移") && index > 0)
            {
                (route.Steps[index], route.Steps[index - 1]) = (route.Steps[index - 1], route.Steps[index]);
                if (selectedStepIndex == index)
                    selectedStepIndex = index - 1;
                else if (selectedStepIndex == index - 1)
                    selectedStepIndex = index;
            }

            if (ImGui.MenuItem("下移") && index < route.Steps.Count - 1)
            {
                (route.Steps[index], route.Steps[index + 1]) = (route.Steps[index + 1], route.Steps[index]);
                if (selectedStepIndex == index)
                    selectedStepIndex = index + 1;
                else if (selectedStepIndex == index + 1)
                    selectedStepIndex = index;
            }
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
        ImGui.AlignTextToFramePadding();
        ImGui.Text("目标预设:");

        ImGui.SameLine();
        ImGui.SetNextItemWidth(200f * ImGuiHelpers.GlobalScale);

        if (ImGui.BeginCombo("###TargetPreset", step.PresetName))
        {
            foreach (var preset in Service.Config.Presets)
            {
                if (ImGui.Selectable(preset.Name, step.PresetName == preset.Name))
                    step.PresetName = preset.Name;
            }

            ImGui.EndCombo();
        }

        ImGui.Spacing();

        // 副本选项配置
        ImGui.TextColored(KnownColor.LightSkyBlue.ToVector4(), "副本选项:");
        using (ImRaii.PushIndent())
            DrawDutyOptions(step.DutyOptions);

        ImGui.NewLine();

        // 预设执行结束后的动作配置
        ImGui.AlignTextToFramePadding();
        ImGui.Text("预设执行结束后:");

        using (ImRaii.PushIndent())
        {
            ImGui.AlignTextToFramePadding();
            ImGui.Text("执行动作:");

            ImGui.SameLine();
            ImGui.SetNextItemWidth(150f * ImGuiHelpers.GlobalScale);

            if (ImGui.BeginCombo("###AfterPresetAction", step.AfterPresetAction.GetDescription()))
            {
                foreach (var action in Enum.GetValues<RouteStepActionType>())
                {
                    var actionName = action.GetDescription();
                    if (ImGui.Selectable(actionName, step.AfterPresetAction == action))
                        step.AfterPresetAction = action;
                }

                ImGui.EndCombo();
            }

            // 如果是跳转动作，显示跳转索引输入
            if (step.AfterPresetAction == RouteStepActionType.JumpToStep)
            {
                ImGui.SameLine();
                ImGui.Text("目标步骤:");

                ImGui.SameLine();
                ImGui.SetNextItemWidth(150f * ImGuiHelpers.GlobalScale);
                var tempJumpIndex = step.AfterPresetJumpIndex;
                if (ImGui.InputInt("###AfterPresetJumpIndex", ref tempJumpIndex))
                    step.AfterPresetJumpIndex = tempJumpIndex;
            }
        }

        // 提示文本
        ImGui.TextColored(KnownColor.Yellow.Vector(), "提示: 每个预设均仅会执行一次, 有固定次数需要请结合条件判断步骤");
    }

    private static void DrawConditionCheckConfig(RouteStep step)
    {
        using (var table = ImRaii.Table("ConditionConfigTable", 2, ImGuiTableFlags.SizingFixedFit))
        {
            if (table)
            {
                ImGui.TableSetupColumn("Label",   ImGuiTableColumnFlags.WidthFixed, 100f * ImGuiHelpers.GlobalScale);
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
                    foreach (var conditionType in Enum.GetValues<ConditionType>())
                    {
                        var conditionName = conditionType.GetDescription();
                        if (ImGui.Selectable(conditionName, step.ConditionType == conditionType))
                            step.ConditionType = conditionType;
                    }

                    ImGui.EndCombo();
                }

                // 额外ID（成就ID或物品ID）
                if (step.ConditionType is ConditionType.AchievementCount or ConditionType.ItemCount)
                {
                    ImGui.TableNextRow();
                    ImGui.TableSetColumnIndex(0);
                    ImGui.AlignTextToFramePadding();
                    ImGui.Text(step.ConditionType == ConditionType.AchievementCount ? "成就 ID:" : "物品 ID:");

                    ImGui.TableSetColumnIndex(1);
                    ImGui.SetNextItemWidth(200f * ImGuiHelpers.GlobalScale);
                    var extraID = step.ExtraID;
                    if (ImGui.InputInt("###ExtraId", ref extraID))
                        step.ExtraID = extraID;

                    if(extraID != 0)
                    {
                        switch (step.ConditionType)
                        {
                            case ConditionType.AchievementCount when LuminaGetter.TryGetRow((uint)extraID, out Achievement achievementRow):
                                ImGui.SameLine();
                                ImGui.TextUnformatted($"{achievementRow.Name}");
                                ImGuiOm.TooltipHover($"{achievementRow.Description}");
                                break;
                            
                            case ConditionType.ItemCount when LuminaGetter.TryGetRow((uint)extraID, out Item itemRow):
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
                ImGui.SetNextItemWidth(100f * ImGuiHelpers.GlobalScale);

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
                ImGui.SetNextItemWidth(150f * ImGuiHelpers.GlobalScale);
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
        ImGui.SetNextItemWidth(150f * ImGuiHelpers.GlobalScale);

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
            ImGui.SetNextItemWidth(150f * ImGuiHelpers.GlobalScale);
            var tempJumpIndex = jumpIndex;
            if (ImGui.InputInt($"###JumpIndex{prefix}", ref tempJumpIndex))
                jumpIndex = tempJumpIndex;
        }
    }

    private static void DrawDutyOptions(DutyOptions dutyOptions)
    {
        using var group = ImRaii.Group();

        using (ImRaii.Group())
        {
            var option = dutyOptions.ContentsFinderOption;

            var unrestrictedParty = option.UnrestrictedParty;

            if (ImGui.Checkbox("解除限制", ref unrestrictedParty))
            {
                option.UnrestrictedParty         = unrestrictedParty;
                dutyOptions.ContentsFinderOption = option;
            }

            ImGui.SameLine();
            var levelSync = option.LevelSync;

            if (ImGui.Checkbox("等级同步", ref levelSync))
            {
                option.LevelSync                 = levelSync;
                dutyOptions.ContentsFinderOption = option;
            }

            ImGui.SameLine();
            var minimalIL = option.MinimalIL;

            if (ImGui.Checkbox("最低品级", ref minimalIL))
            {
                option.MinimalIL                 = minimalIL;
                dutyOptions.ContentsFinderOption = option;
            }

            ImGui.SameLine();
            var silenceEcho = option.SilenceEcho;

            if (ImGui.Checkbox("超越之力无效化", ref silenceEcho))
            {
                option.SilenceEcho               = silenceEcho;
                dutyOptions.ContentsFinderOption = option;
            }

            ImGui.SameLine();
            var supply = option.Supply;

            if (ImGui.Checkbox("中途加入", ref supply))
            {
                option.Supply                    = supply;
                dutyOptions.ContentsFinderOption = option;
            }

            var lootRule = option.LootRules;
            var isFirst  = true;

            foreach (var (loot, loc) in Main.LootRuleLOC)
            {
                if (!isFirst)
                    ImGui.SameLine();
                isFirst = false;

                if (ImGui.RadioButton($"{loc}##{loot}", loot == lootRule))
                {
                    option.LootRules                 = loot;
                    dutyOptions.ContentsFinderOption = option;
                }
            }
        }

        ImGui.SetNextItemWidth(200f * ImGuiHelpers.GlobalScale);

        using (var combo = ImRaii.Combo("副本入口###ContentEntryCombo", dutyOptions.ContentEntryType.GetDescription()))
        {
            if (combo)
            {
                foreach (var entryType in Enum.GetValues<ContentEntryType>())
                {
                    if (ImGui.Selectable(entryType.GetDescription(), entryType == dutyOptions.ContentEntryType))
                        dutyOptions.ContentEntryType = entryType;
                }
            }
        }

        ImGuiOm.TooltipHover("单人进入多变迷宫:\n\t勾选解除限制, 入口选择一般副本");
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
            Chat($"导入路线失败: {ex.Message}", Main.UTHPrefix);
            return null;
        }
    }

    private static void ExportRouteToClipboard(Route route)
    {
        try
        {
            var json = JsonConvert.SerializeObject(route, Formatting.Indented);
            ImGui.SetClipboardText(json);

            Chat("路线已导出到剪贴板", Main.UTHPrefix);
        }
        catch (Exception ex)
        {
            Chat($"导出路线失败: {ex.Message}", Main.UTHPrefix);
        }
    }

    public void Dispose() { }
}
