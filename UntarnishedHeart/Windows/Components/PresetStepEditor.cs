using System.Numerics;
using System.Runtime.CompilerServices;
using UntarnishedHeart.Execution.Condition;
using UntarnishedHeart.Execution.Enums;
using UntarnishedHeart.Execution.ExecuteAction;
using UntarnishedHeart.Execution.ExecuteAction.Enums;
using UntarnishedHeart.Execution.Preset;
using UntarnishedHeart.Execution.Preset.Enums;
using UntarnishedHeart.Windows.Helpers;

namespace UntarnishedHeart.Windows.Components;

internal static class PresetStepEditor
{
    private static readonly ConditionalWeakTable<PresetStep, EditorState> EditorStates = [];

    public static void Draw(PresetStep step, ref int i, List<PresetStep> steps, StepEditorSharedState sharedState)
    {
        var state = EditorStates.GetValue(step, static _ => new EditorState());

        using var id    = ImRaii.PushId($"Step-{i}");
        using var group = ImRaii.Group();

        var stepName = step.Name;
        ImGuiOm.CompLabelLeft("名称:", -1f, () => ImGui.InputText("###StepNameInput", ref stepName, 128));
        if (ImGui.IsItemDeactivatedAfterEdit())
            step.Name = stepName;

        var stepRemark = step.Remark;
        ImGuiOm.CompLabelLeft("备注:", -1f, () => ImGui.InputText("###StepRemarkInput", ref stepRemark, 2048));
        if (ImGui.IsItemDeactivatedAfterEdit())
            step.Remark = stepRemark;

        ImGui.Spacing();

        using var child = ImRaii.Child
        (
            "StepContentChild",
            ImGui.GetContentRegionAvail() - ImGui.GetStyle().ItemSpacing,
            false,
            ImGuiWindowFlags.NoScrollbar |
            ImGuiWindowFlags.NoScrollWithMouse
        );
        if (!child) return;

        using var color  = ImRaii.PushColor(ImGuiCol.ChildBg, Vector4.Zero);
        using var tabBar = ImRaii.TabBar("###StepContentTabBar");
        if (!tabBar) return;

        var enterSelectedIndex = state.EnterSelectedIndex;
        DrawPhaseTab("进入阶段", "一般用来存储是否要进入该步骤的动作与判断", step.EnterActions, PresetStepPhase.Enter, ref enterSelectedIndex, state, sharedState);
        state.EnterSelectedIndex = enterSelectedIndex;

        var bodySelectedIndex = state.BodySelectedIndex;
        DrawPhaseTab("进行阶段", "一般用来存储该步骤的实际逻辑", step.BodyActions, PresetStepPhase.Body, ref bodySelectedIndex, state, sharedState);
        state.BodySelectedIndex = bodySelectedIndex;

        var exitSelectedIndex = state.ExitSelectedIndex;
        DrawPhaseTab("离开阶段", "一般用来存储是否要离开该步骤的动作与判断", step.ExitActions, PresetStepPhase.Exit, ref exitSelectedIndex, state, sharedState);
        state.ExitSelectedIndex = exitSelectedIndex;

        DrawReorderButtons(ref i, steps);
    }

    private static unsafe void DrawPhaseTab
    (
        string                              title,
        string                              decription,
        List<ExecuteActionBase>             actions,
        PresetStepPhase                     phase,
        ref int                             selectedIndex,
        EditorState                         state,
        StepEditorSharedState               sharedState
    )
    {
        using var tab = ImRaii.TabItem(title);
        ImGuiOm.TooltipHover(decription);
        if (!tab)
            return;

        selectedIndex = CollectionToolbar.NormalizeSelectedIndex(selectedIndex, actions.Count);

        using var table = ImRaii.Table($"{phase}ActionsTable", 2, ImGuiTableFlags.Resizable | ImGuiTableFlags.BordersInnerV);
        if (!table) return;

        ImGui.TableSetupColumn("ActionsList",   ImGuiTableColumnFlags.WidthFixed, 200f * GlobalUIScale);
        ImGui.TableSetupColumn("ActionDetails", ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableNextRow();

        ImGui.TableSetColumnIndex(0);
        if (ImGuiOm.ButtonStretch($"添加动作###{phase}AddAction"))
            actions.Add(ExecuteActionBase.CreateDefaultAction(ExecuteActionKind.Wait));

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        using (var child = ImRaii.Child
               (
                   $"{phase}ActionsSelectChild",
                   ImGui.GetContentRegionAvail(),
                   true,
                   ImGuiWindowFlags.NoScrollbar |
                   ImGuiWindowFlags.NoScrollWithMouse
               ))
        {
            if (child)
            {
                for (var actionIndex = 0; actionIndex < actions.Count; actionIndex++)
                {
                    var action     = actions[actionIndex];
                    var actionName = $"{actionIndex}. {action.Name}";

                    if (ImGui.Selectable(actionName, actionIndex == selectedIndex, ImGuiSelectableFlags.AllowDoubleClick))
                        selectedIndex = actionIndex;

                    if (ImGui.BeginDragDropSource(ImGuiDragDropFlags.None))
                    {
                        ImGui.SetDragDropPayload($"ACTION_REORDER_{phase}", BitConverter.GetBytes(actionIndex));
                        ImGui.Text(actionName);
                        ImGui.EndDragDropSource();
                    }

                    if (ImGui.BeginDragDropTarget())
                    {
                        var payload = ImGui.AcceptDragDropPayload($"ACTION_REORDER_{phase}");

                        if (!payload.IsNull && payload.Data != null)
                        {
                            var sourceIndex = *(int*)payload.Data;

                            if (sourceIndex != actionIndex && sourceIndex >= 0 && sourceIndex < actions.Count)
                            {
                                (actions[sourceIndex], actions[actionIndex]) = (actions[actionIndex], actions[sourceIndex]);

                                if (selectedIndex == sourceIndex)
                                    selectedIndex = actionIndex;
                                else if (selectedIndex == actionIndex)
                                    selectedIndex = sourceIndex;
                            }
                        }

                        ImGui.EndDragDropTarget();
                    }

                    DrawActionContextMenu(actions, sharedState, ref selectedIndex, actionIndex, action, phase);
                }
            }
        }

        ImGui.TableSetColumnIndex(1);
        using var detailsChild = ImRaii.Child($"{phase}ActionsDetailsChild", ImGui.GetContentRegionAvail(), true, ImGuiWindowFlags.NoBackground);
        if (!detailsChild) return;

        if (selectedIndex < 0 || selectedIndex >= actions.Count)
        {
            ImGui.TextDisabled("请选择一个动作进行编辑");
            return;
        }

        var currentAction = actions[selectedIndex];
        actions[selectedIndex] = DrawActionEditor(currentAction, phase, selectedIndex);
    }

    private sealed class EditorState
    {
        public int EnterSelectedIndex { get; set; } = -1;
        public int BodySelectedIndex  { get; set; } = -1;
        public int ExitSelectedIndex  { get; set; } = -1;
    }

    private static void DrawActionContextMenu
    (
        List<ExecuteActionBase>             actions,
        StepEditorSharedState               sharedState,
        ref int                             selectedIndex,
        int                                 actionIndex,
        ExecuteActionBase                   action,
        PresetStepPhase                     phase
    )
    {
        var contextOperation = StepOperationType.Pass;

        if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
            ImGui.OpenPopup($"ActionContentMenu_{phase}_{actionIndex}");

        using var context = ImRaii.ContextPopupItem($"ActionContentMenu_{phase}_{actionIndex}");
        if (!context) return;

        ImGui.Text($"第 {actionIndex} 个动作: {action.Name}");
        ImGui.Separator();

        if (ImGui.MenuItem("复制"))
            sharedState.ActionToCopy = ExecuteActionBase.Copy(action);

        if (sharedState.ActionToCopy != null)
        {
            if (ImGui.MenuItem("粘贴至本条"))
                contextOperation = StepOperationType.Paste;

            if (ImGui.MenuItem("向上插入粘贴"))
                contextOperation = StepOperationType.PasteUp;

            if (ImGui.MenuItem("向下插入粘贴"))
                contextOperation = StepOperationType.PasteDown;
        }

        if (ImGui.MenuItem("删除"))
            contextOperation = StepOperationType.Delete;

        if (actionIndex > 0 && ImGui.MenuItem("上移"))
            contextOperation = StepOperationType.MoveUp;

        if (actionIndex < actions.Count - 1 && ImGui.MenuItem("下移"))
            contextOperation = StepOperationType.MoveDown;

        ImGui.Separator();

        if (ImGui.MenuItem("向上插入新动作"))
            contextOperation = StepOperationType.InsertUp;

        if (ImGui.MenuItem("向下插入新动作"))
            contextOperation = StepOperationType.InsertDown;

        ImGui.Separator();

        if (ImGui.MenuItem("复制并插入本条"))
            contextOperation = StepOperationType.PasteCurrent;

        selectedIndex = CollectionOperationHelper.Apply
        (
            actions,
            actionIndex,
            contextOperation,
            selectedIndex,
            () => ExecuteActionBase.CreateDefaultAction(ExecuteActionKind.Wait),
            sharedState.ActionToCopy == null ? null : () => ExecuteActionBase.Copy(sharedState.ActionToCopy),
            () => ExecuteActionBase.Copy(action)
        );
    }

    private static ExecuteActionBase DrawActionEditor(ExecuteActionBase action, PresetStepPhase phase, int actionIndex)
    {
        using var id = ImRaii.PushId($"{phase}-Action-{actionIndex}");

        DrawActionMetadataEditor(action);

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        var current = DrawActionTypeSelector(action);

        ImGui.Spacing();

        current.Draw();

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        ImGui.TextColored(KnownColor.LightSkyBlue.ToVector4(), "条件组");
        current.Condition.Draw();

        return current;
    }

    private static void DrawActionMetadataEditor(ExecuteActionBase action)
    {
        var actionName = action.Name;
        ImGuiOm.CompLabelLeft("名称:", -1f, () => ImGui.InputText("###ActionNameInput", ref actionName, 128));
        if (ImGui.IsItemDeactivatedAfterEdit())
            action.Name = actionName;

        var actionRemark = action.Remark;
        ImGuiOm.CompLabelLeft("备注:", -1f, () => ImGui.InputText("###ActionRemarkInput", ref actionRemark, 2048));
        if (ImGui.IsItemDeactivatedAfterEdit())
            action.Remark = actionRemark;
    }

    private static ExecuteActionBase DrawActionTypeSelector(ExecuteActionBase current)
    {
        ImGui.SetNextItemWidth(240f * GlobalUIScale);
        using var combo = ImRaii.Combo("执行动作###ActionKindCombo", current.Kind.GetDescription(), ImGuiComboFlags.HeightLargest);
        if (!combo)
            return current;

        foreach (var actionKind in Enum.GetValues<ExecuteActionKind>())
        {
            if (!ImGui.Selectable(actionKind.GetDescription(), current.Kind == actionKind))
                continue;

            if (current.Kind == actionKind)
                return current;

            var keepCustomName = !string.IsNullOrEmpty(current.Name) &&
                                 !string.Equals(current.Name, current.GetDefaultName(), StringComparison.Ordinal);
            var next = ExecuteActionBase.CreateDefaultAction(actionKind);
            if (keepCustomName)
                next.Name = current.Name;

            next.Remark    = current.Remark;
            next.Condition = ConditionCollection.Copy(current.Condition);
            return next;
        }

        return current;
    }

    private static void DrawReorderButtons(ref int i, List<PresetStep> steps)
    {
        if (i > 0)
        {
            if (ImGui.TabItemButton("↑"))
            {
                var index = i - 1;
                steps.Swap(i, index);
                i = index;
            }

            ImGuiOm.TooltipHover("上移步骤");
        }

        if (i < steps.Count - 1)
        {
            if (ImGui.TabItemButton("↓"))
            {
                var index = i + 1;
                steps.Swap(i, index);
                i = index;
            }

            ImGuiOm.TooltipHover("下移步骤");
        }
    }
}
