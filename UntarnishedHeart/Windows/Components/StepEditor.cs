using System.Runtime.CompilerServices;
using UntarnishedHeart.Execution.Condition;
using UntarnishedHeart.Execution.Enums;
using UntarnishedHeart.Execution.ExecuteAction;
using UntarnishedHeart.Execution.ExecuteAction.Enums;
using UntarnishedHeart.Execution.Preset;
using UntarnishedHeart.Execution.Preset.Enums;
using UntarnishedHeart.Windows.Helpers;

namespace UntarnishedHeart.Windows.Components;

internal static class StepEditor
{
    private static readonly ConditionalWeakTable<PresetStep, EditorState> EditorStates = [];

    public static void DrawStepMetadata(PresetStep step)
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

    public static List<ExecuteActionBase> GetPhaseActions(PresetStep step, PresetStepPhase phase) =>
        phase switch
        {
            PresetStepPhase.Enter => step.EnterActions,
            PresetStepPhase.Body  => step.BodyActions,
            PresetStepPhase.Exit  => step.ExitActions,
            _                     => throw new InvalidOperationException($"不支持的阶段: {phase}")
        };

    public static int NormalizeActionSelection(PresetStep step, PresetStepPhase phase)
    {
        var state         = EditorStates.GetValue(step, static _ => new EditorState());
        var actions       = GetPhaseActions(step, phase);
        var selectedIndex = CollectionToolbar.NormalizeSelectedIndex(state.GetSelectedIndex(phase), actions.Count);
        state.SetSelectedIndex(phase, selectedIndex);
        return selectedIndex;
    }

    public static int GetActionSelection(PresetStep step, PresetStepPhase phase)
    {
        var state = EditorStates.GetValue(step, static _ => new EditorState());
        return state.GetSelectedIndex(phase);
    }

    public static void SetActionSelection(PresetStep step, PresetStepPhase phase, int selectedIndex)
    {
        var state   = EditorStates.GetValue(step, static _ => new EditorState());
        var actions = GetPhaseActions(step, phase);
        state.SetSelectedIndex(phase, CollectionToolbar.NormalizeSelectedIndex(selectedIndex, actions.Count));
    }

    public static bool TrySelectFirstAction(PresetStep step, PresetStepPhase phase, out int selectedIndex)
    {
        var actions = GetPhaseActions(step, phase);

        if (actions.Count == 0)
        {
            selectedIndex = -1;
            SetActionSelection(step, phase, selectedIndex);
            return false;
        }

        selectedIndex = 0;
        SetActionSelection(step, phase, selectedIndex);
        return true;
    }

    public static bool DrawSelectedActionEditor(PresetStep step, PresetStepPhase phase, ref int selectedIndex)
    {
        var actions = GetPhaseActions(step, phase);
        selectedIndex = CollectionToolbar.NormalizeSelectedIndex(selectedIndex, actions.Count);
        SetActionSelection(step, phase, selectedIndex);

        if (selectedIndex < 0 || selectedIndex >= actions.Count)
        {
            ImGui.TextDisabled("当前阶段暂无执行动作");
            return false;
        }

        var currentIndex  = selectedIndex;
        var currentAction = actions[currentIndex];
        DrawActionEditor(currentAction, phase, currentIndex, next => ReplaceAction(actions, currentAction, next));
        return true;
    }


    private sealed class EditorState
    {
        public int EnterSelectedIndex { get; set; } = -1;
        public int BodySelectedIndex  { get; set; } = -1;
        public int ExitSelectedIndex  { get; set; } = -1;

        public int GetSelectedIndex(PresetStepPhase phase) =>
            phase switch
            {
                PresetStepPhase.Enter => EnterSelectedIndex,
                PresetStepPhase.Body  => BodySelectedIndex,
                PresetStepPhase.Exit  => ExitSelectedIndex,
                _                     => -1
            };

        public void SetSelectedIndex(PresetStepPhase phase, int index)
        {
            switch (phase)
            {
                case PresetStepPhase.Enter:
                    EnterSelectedIndex = index;
                    break;
                case PresetStepPhase.Body:
                    BodySelectedIndex = index;
                    break;
                case PresetStepPhase.Exit:
                    ExitSelectedIndex = index;
                    break;
            }
        }
    }

    public static void DrawActionContextMenu
    (
        PresetStep            step,
        PresetStepPhase       phase,
        int                   actionIndex,
        StepEditorSharedState sharedState,
        string                popupID,
        Action<int>?          startFromAction = null
    )
    {
        var actions       = GetPhaseActions(step, phase);
        var selectedIndex = NormalizeActionSelection(step, phase);
        if (actionIndex < 0 || actionIndex >= actions.Count)
            return;

        DrawActionContextMenu(actions, sharedState, ref selectedIndex, actionIndex, actions[actionIndex], phase, popupID, startFromAction);
        SetActionSelection(step, phase, selectedIndex);
    }

    private static void DrawActionContextMenu
    (
        List<ExecuteActionBase> actions,
        StepEditorSharedState   sharedState,
        ref int                 selectedIndex,
        int                     actionIndex,
        ExecuteActionBase       action,
        PresetStepPhase         phase,
        string?                 popupID         = null,
        Action<int>?            startFromAction = null
    )
    {
        var contextOperation = StepOperationType.Pass;

        if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
            ImGui.OpenPopup(popupID ?? $"ActionContentMenu_{phase}_{actionIndex}");

        using var context = ImRaii.ContextPopupItem(popupID ?? $"ActionContentMenu_{phase}_{actionIndex}");
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
            sharedState.ActionToCopy == null ? null : () => ExecuteActionBase.Copy(sharedState.ActionToCopy),
            () => ExecuteActionBase.Copy(action)
        );
    }

    private static void DrawActionEditor(ExecuteActionBase action, PresetStepPhase phase, int actionIndex, Action<ExecuteActionBase> replaceCurrent)
    {
        using var id = ImRaii.PushId($"{phase}-Action-{actionIndex}");

        DrawActionMetadataEditor(action);

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        using var tabBar = ImRaii.TabBar("ExecuteAction-ConditionGroup");

        using (var item = ImRaii.TabItem("执行动作"))
        {
            if (item)
            {
                ImGui.Spacing();
                DrawActionTypeSelector(action, replaceCurrent);
                action.Draw();
            }
        }

        using (var item = ImRaii.TabItem("条件组"))
        {
            if (item)
                action.Condition.Draw();
        }
    }

    private static void ReplaceAction(List<ExecuteActionBase> actions, ExecuteActionBase current, ExecuteActionBase next)
    {
        for (var i = 0; i < actions.Count; i++)
        {
            if (!ReferenceEquals(actions[i], current))
                continue;

            actions[i] = next;
            return;
        }
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

    private static void DrawActionTypeSelector(ExecuteActionBase current, Action<ExecuteActionBase> replaceCurrent)
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
