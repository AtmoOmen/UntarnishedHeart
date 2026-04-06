using System.Numerics;
using System.Runtime.CompilerServices;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Interface.Utility;
using FFXIVClientStructs.FFXIV.Client.Game;
using UntarnishedHeart.Execution.Condition;
using UntarnishedHeart.Execution.Enums;
using UntarnishedHeart.Execution.ExecuteAction;
using UntarnishedHeart.Execution.ExecuteAction.Enums;
using UntarnishedHeart.Execution.ExecuteAction.Implementations;
using UntarnishedHeart.Execution.Models;
using UntarnishedHeart.Execution.Preset;
using UntarnishedHeart.Execution.Preset.Enums;
using UntarnishedHeart.Execution.Preset.Helpers;
using UntarnishedHeart.Windows.Helpers;

namespace UntarnishedHeart.Windows.Components;

internal static class PresetStepEditor
{
    private static readonly ConditionalWeakTable<PresetStep, EditorState> EditorStates = [];

    public static void Draw(PresetStep step, ref int i, List<PresetStep> steps)
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
        DrawPhaseTab("进入阶段", "一般用来存储是否要进入该步骤的动作与判断", step.EnterActions, PresetStepPhase.Enter, ref enterSelectedIndex, state);
        state.EnterSelectedIndex = enterSelectedIndex;

        var bodySelectedIndex = state.BodySelectedIndex;
        DrawPhaseTab("进行阶段", "一般用来存储该步骤的实际逻辑", step.BodyActions, PresetStepPhase.Body, ref bodySelectedIndex, state);
        state.BodySelectedIndex = bodySelectedIndex;

        var exitSelectedIndex = state.ExitSelectedIndex;
        DrawPhaseTab("离开阶段", "一般用来存储是否要离开该步骤的动作与判断", step.ExitActions, PresetStepPhase.Exit, ref exitSelectedIndex, state);
        state.ExitSelectedIndex = exitSelectedIndex;

        DrawReorderButtons(ref i, steps);
    }

    private static unsafe void DrawPhaseTab
    (
        string              title,
        string              decription,
        List<ExecuteAction> actions,
        PresetStepPhase     phase,
        ref int             selectedIndex,
        EditorState         state
    )
    {
        using var tab = ImRaii.TabItem(title);
        ImGuiOm.TooltipHover(decription);
        if (!tab)
            return;

        selectedIndex = CollectionToolbar.NormalizeSelectedIndex(selectedIndex, actions.Count);

        using var table = ImRaii.Table($"{phase}ActionsTable", 2, ImGuiTableFlags.Resizable | ImGuiTableFlags.BordersInnerV);
        if (!table) return;

        ImGui.TableSetupColumn("ActionsList",   ImGuiTableColumnFlags.WidthFixed, 300f * GlobalUIScale);
        ImGui.TableSetupColumn("ActionDetails", ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableNextRow();

        ImGui.TableSetColumnIndex(0);
        if (ImGuiOm.ButtonSelectable($"添加动作###{phase}AddAction"))
            actions.Add(new WaitMillisecondsAction());

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
                    var actionName = $"{actionIndex}. {action.Kind.GetDescription()}";

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

                    DrawActionContextMenu(actions, state, ref selectedIndex, actionIndex, action, phase);
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
        public int            EnterSelectedIndex { get; set; } = -1;
        public int            BodySelectedIndex  { get; set; } = -1;
        public int            ExitSelectedIndex  { get; set; } = -1;
        public ExecuteAction? ActionToCopy       { get; set; }
    }

    private static void DrawActionContextMenu
    (
        List<ExecuteAction> actions,
        EditorState         state,
        ref int             selectedIndex,
        int                 actionIndex,
        ExecuteAction       action,
        PresetStepPhase     phase
    )
    {
        var contextOperation = StepOperationType.Pass;

        if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
            ImGui.OpenPopup($"ActionContentMenu_{phase}_{actionIndex}");

        using var context = ImRaii.ContextPopupItem($"ActionContentMenu_{phase}_{actionIndex}");
        if (!context) return;

        ImGui.Text($"第 {actionIndex} 个动作: {action.Kind.GetDescription()}");
        ImGui.Separator();

        if (ImGui.MenuItem("复制"))
            state.ActionToCopy = ExecuteAction.Copy(action);

        if (state.ActionToCopy != null)
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
            () => new WaitMillisecondsAction(),
            state.ActionToCopy == null ? null : () => ExecuteAction.Copy(state.ActionToCopy),
            () => ExecuteAction.Copy(action)
        );
    }

    private static ExecuteAction DrawActionEditor(ExecuteAction action, PresetStepPhase phase, int actionIndex)
    {
        using var id = ImRaii.PushId($"{phase}-Action-{actionIndex}");

        var current = DrawActionTypeSelector(action);
        
        ImGui.Spacing();
        
        DrawActionBody(current);

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();
        
        ImGui.TextColored(KnownColor.LightSkyBlue.ToVector4(), "条件组");
        current.Condition.Draw();

        return current;
    }

    private static ExecuteAction DrawActionTypeSelector(ExecuteAction current)
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

            var next = CreateDefaultAction(actionKind);
            next.Condition = ConditionCollection.Copy(current.Condition);
            return next;
        }

        return current;
    }

    private static void DrawActionBody(ExecuteAction action)
    {
        switch (action)
        {
            case WaitMillisecondsAction waitMilliseconds:
                DrawWaitMilliseconds(waitMilliseconds);
                break;
            case JumpToStepAction jumpToStep:
                DrawJumpToStep(jumpToStep);
                break;
            case RestartCurrentStepAction:
            case RestartCurrentActionAction:
            case LeaveDutyAndEndAction:
            case LeaveDutyAndRestartAction:
            case InteractNearestObjectAction:
                ImGui.TextDisabled("此动作无需额外参数");
                break;
            case JumpToActionAction jumpToAction:
                DrawJumpToAction(jumpToAction);
                break;
            case TextCommandAction textCommand:
                DrawTextCommand(textCommand);
                break;
            case SelectTargetAction selectTarget:
                DrawTargetSelector(selectTarget.Selector, "SelectTarget");
                break;
            case InteractTargetAction interactTarget:
                DrawTargetSelector(interactTarget.Selector, "InteractTarget");
                var openObjectInteraction = interactTarget.OpenObjectInteraction;
                if (ImGui.Checkbox("尝试打开对象交互###OpenObjectInteraction", ref openObjectInteraction))
                    interactTarget.OpenObjectInteraction = openObjectInteraction;
                break;
            case UseActionExecuteAction useAction:
                DrawUseAction(useAction);
                break;
            case MoveToPositionAction moveToPosition:
                DrawMoveToPosition(moveToPosition);
                break;
        }
    }

    private static void DrawWaitMilliseconds(WaitMillisecondsAction action)
    {
        var milliseconds = action.Milliseconds;
        ImGui.SetNextItemWidth(240f * GlobalUIScale);
        if (ImGui.InputInt("等待毫秒###WaitMillisecondsInput", ref milliseconds))
            action.Milliseconds = Math.Max(0, milliseconds);
    }

    private static void DrawJumpToStep(JumpToStepAction action)
    {
        var stepIndex = action.StepIndex;
        ImGui.SetNextItemWidth(240f * GlobalUIScale);
        if (ImGui.InputInt("目标步骤索引###JumpToStepInput", ref stepIndex))
            action.StepIndex = stepIndex;
    }

    private static void DrawJumpToAction(JumpToActionAction action)
    {
        var actionIndex = action.ActionIndex;
        ImGui.SetNextItemWidth(240f * GlobalUIScale);
        if (ImGui.InputInt("目标动作索引###JumpToActionInput", ref actionIndex))
            action.ActionIndex = actionIndex;
    }

    private static void DrawTextCommand(TextCommandAction action)
    {
        var commands    = action.Commands;
        var inputHeight = Math.Max(ImGui.GetTextLineHeightWithSpacing() * 6f, 120f * GlobalUIScale);
        if (ImGui.InputTextMultiline("###CommandsInput", ref commands, 4096, new(-1f, inputHeight)))
            action.Commands = commands;
    }

    private static void DrawUseAction(UseActionExecuteAction action)
    {
        DrawActionReference(action.Action);

        ImGui.Separator();
        ImGui.TextColored(KnownColor.LightSkyBlue.ToVector4(), "目标:");
        DrawTargetSelector(action.TargetSelector, "UseActionTarget");

        var useLocation = action.UseLocation;
        if (ImGui.Checkbox("按地面坐标释放###UseLocation", ref useLocation))
            action.UseLocation = useLocation;

        if (action.UseLocation)
        {
            var location = action.Location;
            if (ImGui.InputFloat3("地面坐标###UseActionLocation", ref location))
                action.Location = location;

            ImGui.SameLine();
            if (ImGuiOm.ButtonIcon("UseActionGetCurrentPosition", FontAwesomeIcon.Bullseye, "取当前位置", true) &&
                DService.Instance().ObjectTable.LocalPlayer is { } localPlayer)
                action.Location = localPlayer.Position;
        }
    }

    private static void DrawMoveToPosition(MoveToPositionAction action)
    {
        var moveType = action.MoveType;

        using (var combo = ImRaii.Combo("###MoveTypeCombo", moveType.ToString()))
        {
            if (combo)
            {
                foreach (var candidate in Enum.GetValues<MoveType>())
                {
                    if (ImGui.Selectable(candidate.ToString(), moveType == candidate))
                        moveType = candidate;
                }
            }
        }

        action.MoveType = moveType;

        var position = action.Position;
        if (ImGui.InputFloat3("位置###MovePositionInput", ref position))
            action.Position = position;

        ImGui.SameLine();
        if (ImGuiOm.ButtonIcon("MoveGetPosition", FontAwesomeIcon.Bullseye, "取当前位置", true) &&
            DService.Instance().ObjectTable.LocalPlayer is { } localPlayer)
            action.Position = localPlayer.Position;

        var waitForArrival = action.WaitForArrival;
        if (ImGui.Checkbox("等待接近后再继续###WaitForArrivalInput", ref waitForArrival))
            action.WaitForArrival = waitForArrival;
        ImGuiOm.HelpMarker("不推荐使用这一选项, 目前仅做兼容性功能提供, 后续可能会删除\n建议使用条件组, 设置处理类型为\"持续\", 新增条件 \"坐标范围\" 来实现更加精细的判断");
    }

    private static void DrawActionReference(ActionReference reference)
    {
        ImGui.TextColored(KnownColor.LightSkyBlue.ToVector4(), "技能类型:");
        ImGui.SameLine();

        using (var combo = ImRaii.Combo("###ActionTypeCombo", reference.ActionType.ToString()))
        {
            if (combo)
            {
                foreach (var actionType in Enum.GetValues<ActionType>())
                {
                    if (ImGui.Selectable(actionType.ToString(), reference.ActionType == actionType))
                        reference.ActionType = actionType;
                }
            }
        }

        var actionID = reference.ActionID;
        ImGui.SetNextItemWidth(240f * GlobalUIScale);
        if (ImGui.InputUInt("技能 ID###ActionIdInput", ref actionID))
            reference.ActionID = actionID;
    }

    private static void DrawTargetSelector(TargetSelector selector, string idSuffix)
    {
        ImGui.TextColored(KnownColor.LightSkyBlue.ToVector4(), "目标选择方式:");
        ImGui.SameLine();

        using (var combo = ImRaii.Combo($"###TargetSelectorKind{idSuffix}", selector.Kind.GetDescription(), ImGuiComboFlags.HeightLargest))
        {
            if (combo)
            {
                foreach (var kind in Enum.GetValues<TargetSelectorKind>())
                {
                    if (ImGui.Selectable(kind.GetDescription(), selector.Kind == kind))
                        selector.Kind = kind;
                }
            }
        }

        switch (selector.Kind)
        {
            case TargetSelectorKind.ByObjectKindAndDataID:
                using (var kindCombo = ImRaii.Combo($"###TargetObjectKind{idSuffix}", selector.ObjectKind.ToString()))
                {
                    if (kindCombo)
                    {
                        foreach (var objectKind in Enum.GetValues<ObjectKind>())
                        {
                            if (ImGui.Selectable(objectKind.ToString(), selector.ObjectKind == objectKind))
                                selector.ObjectKind = objectKind;
                        }
                    }
                }

                var dataID = selector.DataID;
                ImGui.SetNextItemWidth(240f * GlobalUIScale);
                if (ImGui.InputUInt($"Data ID###{idSuffix}", ref dataID))
                    selector.DataID = dataID;

                var requireTargetable = selector.RequireTargetable;
                if (ImGui.Checkbox($"要求可选中###{idSuffix}", ref requireTargetable))
                    selector.RequireTargetable = requireTargetable;
                break;

            case TargetSelectorKind.ByEntityID:
                var entityID = selector.EntityID;
                ImGui.SetNextItemWidth(240f * GlobalUIScale);
                if (ImGui.InputUInt($"Entity ID###{idSuffix}", ref entityID))
                    selector.EntityID = entityID;
                break;
        }
    }

    private static ExecuteAction CreateDefaultAction(ExecuteActionKind kind) =>
        kind switch
        {
            ExecuteActionKind.WaitMilliseconds          => new WaitMillisecondsAction(),
            ExecuteActionKind.JumpToStep                => new JumpToStepAction(),
            ExecuteActionKind.RestartCurrentStep        => new RestartCurrentStepAction(),
            ExecuteActionKind.JumpToAction              => new JumpToActionAction(),
            ExecuteActionKind.RestartCurrentAction      => new RestartCurrentActionAction(),
            ExecuteActionKind.LeaveDutyAndEndPreset     => new LeaveDutyAndEndAction(),
            ExecuteActionKind.LeaveDutyAndRestartPreset => new LeaveDutyAndRestartAction(),
            ExecuteActionKind.TextCommand               => new TextCommandAction(),
            ExecuteActionKind.SelectTarget              => new SelectTargetAction(),
            ExecuteActionKind.InteractTarget            => new InteractTargetAction(),
            ExecuteActionKind.InteractNearestObject     => new InteractNearestObjectAction(),
            ExecuteActionKind.UseAction                 => new UseActionExecuteAction(),
            ExecuteActionKind.MoveToPosition            => new MoveToPositionAction(),
            _                                           => new WaitMillisecondsAction()
        };

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
