using UntarnishedHeart.Execution.Condition;
using UntarnishedHeart.Execution.Enums;
using UntarnishedHeart.Execution.ExecuteAction;
using UntarnishedHeart.Execution.ExecuteAction.Enums;
using UntarnishedHeart.Execution.ExecuteAction.Implementations;
using UntarnishedHeart.Execution.Preset;
using UntarnishedHeart.Windows.Helpers;

namespace UntarnishedHeart.Windows.Components;

internal static class StepEditor
{
    public static void DrawStepMetadata
    (
        PresetStep step
    )
    {
        var stepName = step.Name;
        ImGuiOm.CompLabelLeft("名称:", -1f, () => ImGui.InputText("###StepNameInput", ref stepName, 128));
        if (ImGui.IsItemDeactivatedAfterEdit())
            step.Name = stepName;

        var stepRemark = step.Remark;
        ImGuiOm.CompLabelLeft("备注:", -1f, () => ImGui.InputText("###StepRemarkInput", ref stepRemark, 2048));
        if (ImGui.IsItemDeactivatedAfterEdit())
            step.Remark = stepRemark;
    }

    public static int NormalizeActionSelection
    (
        StepTreeEditorState state,
        PresetStep          step,
        int                 stepIndex
    )
    {
        var actions       = step.Actions;
        var selectedIndex = CollectionToolbar.NormalizeSelectedIndex(state.StepActionSelections.GetValueOrDefault(stepIndex, -1), actions.Count);
        state.StepActionSelections[stepIndex] = selectedIndex;
        return selectedIndex;
    }

    public static int GetActionSelection
    (
        StepTreeEditorState state,
        int                 stepIndex
    ) =>
        state.StepActionSelections.GetValueOrDefault(stepIndex, -1);

    public static void SetActionSelection
    (
        StepTreeEditorState state,
        PresetStep          step,
        int                 stepIndex,
        int                 selectedIndex
    )
    {
        var actions = step.Actions;
        state.StepActionSelections[stepIndex] = CollectionToolbar.NormalizeSelectedIndex(selectedIndex, actions.Count);
    }

    public static bool DrawSelectedActionEditor
    (
        StepTreeEditorState state,
        PresetStep          step,
        int                 stepIndex,
        IList<PresetStep>   steps,
        ref int             selectedIndex
    )
    {
        var actions = step.Actions;
        selectedIndex = CollectionToolbar.NormalizeSelectedIndex(selectedIndex, actions.Count);
        SetActionSelection(state, step, stepIndex, selectedIndex);

        if (selectedIndex < 0 || selectedIndex >= actions.Count)
        {
            ImGui.TextDisabled("当前步骤暂无执行动作");
            return false;
        }

        var currentIndex  = selectedIndex;
        var currentAction = actions[currentIndex];
        DrawActionEditor(currentAction, step, steps, currentIndex, next => ReplaceAction(actions, currentAction, next));
        return true;
    }

    public static void DrawActionContextMenu
    (
        StepTreeEditorState   state,
        PresetStep            step,
        int                   stepIndex,
        int                   actionIndex,
        StepEditorSharedState sharedState,
        string                popupID,
        Action<int>?          startFromAction = null,
        Action?               onChanged       = null
    )
    {
        var actions       = step.Actions;
        var selectedIndex = NormalizeActionSelection(state, step, stepIndex);
        if (actionIndex < 0 || actionIndex >= actions.Count)
            return;

        DrawActionContextMenu(actions, sharedState, ref selectedIndex, actionIndex, actions[actionIndex], popupID, startFromAction, onChanged);
        SetActionSelection(state, step, stepIndex, selectedIndex);
    }

    private static void DrawActionContextMenu
    (
        List<ExecuteActionBase> actions,
        StepEditorSharedState   sharedState,
        ref int                 selectedIndex,
        int                     actionIndex,
        ExecuteActionBase       action,
        string?                 popupID         = null,
        Action<int>?            startFromAction = null,
        Action?                 onChanged       = null
    )
    {
        var contextOperation = StepOperationType.Pass;

        if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
            ImGui.OpenPopup(popupID ?? $"ActionContentMenu_{actionIndex}");

        using var context = ImRaii.ContextPopupItem(popupID ?? $"ActionContentMenu_{actionIndex}");
        if (!context) return;

        ImGui.Text($"第 {actionIndex} 个动作: {action.Name}");
        ImGui.Separator();

        if (startFromAction != null && ImGui.MenuItem("从该动作开始执行"))
            startFromAction(actionIndex);

        if (startFromAction != null)
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
            sharedState.ActionToCopy == null ?
                null :
                () => ExecuteActionBase.Copy(sharedState.ActionToCopy),
            () => ExecuteActionBase.Copy(action)
        );

        if (contextOperation != StepOperationType.Pass)
            onChanged?.Invoke();
    }

    public static void DrawSinglePresetActionOverview
    (
        PresetStep        step,
        IList<PresetStep> steps
    )
    {
        if (step.Actions is not [var action] || action is not ExecutePresetAction)
            return;

        DrawActionMetadataEditor(action);

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        var drawContext = new ExecuteActionDrawContext
        {
            Steps   = steps,
            Actions = step.Actions
        };
        action.Draw(drawContext);

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        if (ImGui.CollapsingHeader($"条件组 ({action.Condition.Conditions.Count} 个条件)"))
            action.Condition.Draw();
    }

    private static void DrawActionEditor
    (
        ExecuteActionBase         action,
        PresetStep                step,
        IList<PresetStep>         steps,
        int                       actionIndex,
        Action<ExecuteActionBase> replaceCurrent
    )
    {
        using var id = ImRaii.PushId($"Action-{actionIndex}");

        DrawActionMetadataEditor(action);

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        var drawContext = new ExecuteActionDrawContext
        {
            Steps   = steps,
            Actions = step.Actions
        };
        DrawActionTypeSelector(action, replaceCurrent);
        action.Draw(drawContext);

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        if (ImGui.CollapsingHeader($"条件组 ({action.Condition.Conditions.Count} 个条件)"))
            action.Condition.Draw();
    }

    private static void ReplaceAction
    (
        List<ExecuteActionBase> actions,
        ExecuteActionBase       current,
        ExecuteActionBase       next
    )
    {
        for (var i = 0; i < actions.Count; i++)
        {
            if (!ReferenceEquals(actions[i], current))
                continue;

            actions[i] = next;
            return;
        }
    }

    private static void DrawActionMetadataEditor
    (
        ExecuteActionBase action
    )
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

    private static void DrawActionTypeSelector
    (
        ExecuteActionBase         current,
        Action<ExecuteActionBase> replaceCurrent
    )
    {
        ImGui.SetNextItemWidth(240f * GlobalUIScale);
        var candidates = Enum.GetValues<ExecuteActionKind>();

        using var combo = ImRaii.Combo("执行动作###ActionKindCombo", current.Kind.GetDescription(), ImGuiComboFlags.HeightLargest);
        if (combo)
            ImGui.CloseCurrentPopup();

        if (!ImGui.IsItemClicked())
            return;

        CollectionSelectorWindow.OpenEnum
        (
            "选择执行动作",
            "暂无可选执行动作",
            current.Kind,
            actionKind =>
            {
                if (current.Kind == actionKind)
                    return;

                var keepCustomName = !string.IsNullOrEmpty(current.Name) &&
                                     !string.Equals(current.Name, current.GetDefaultName(), StringComparison.Ordinal);
                var nextAction = ExecuteActionBase.CreateDefaultAction(actionKind);
                if (keepCustomName)
                    nextAction.Name = current.Name;

                nextAction.Remark    = current.Remark;
                nextAction.Condition = ConditionCollection.Copy(current.Condition);
                replaceCurrent(nextAction);
            },
            candidates
        );
    }
}
