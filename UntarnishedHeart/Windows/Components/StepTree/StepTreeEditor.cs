using System.Numerics;
using UntarnishedHeart.Execution.Common;
using UntarnishedHeart.Execution.Enums;
using UntarnishedHeart.Execution.ExecuteAction;
using UntarnishedHeart.Execution.ExecuteAction.Enums;
using UntarnishedHeart.Execution.ExecuteAction.Implementations;
using UntarnishedHeart.Execution.Preset;
using UntarnishedHeart.Windows.Helpers;

namespace UntarnishedHeart.Windows.Components;

internal static class StepTreeEditor
{
    public static void Draw
    (
        string                         idPrefix,
        List<PresetStep>               steps,
        StepTreeEditorState            state,
        StepEditorSharedState          sharedState,
        ExecuteActionRuntimeCursor?    runningCursor,
        Func<PresetStep>               createNewStep,
        Action?                        onCollectionChanged = null,
        StepTreeExecutionStartOptions? executionStartOptions = null
    )
    {
        NormalizeState(steps, state);
        state.CurrentPathTabLabel = BuildCurrentPathLabel(steps, state);

        using var table = ImRaii.Table($"{idPrefix}StepsTreeTable", 2, ImGuiTableFlags.Resizable | ImGuiTableFlags.BordersInnerV);
        if (!table) return;

        ImGui.TableSetupColumn($"{idPrefix}Sidebar", ImGuiTableColumnFlags.WidthFixed, 280f * GlobalUIScale);
        ImGui.TableSetupColumn($"{idPrefix}Detail",  ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableNextRow();

        DrawSidebar(idPrefix, steps, state, sharedState, runningCursor, createNewStep, onCollectionChanged, executionStartOptions);
        DrawDetails(idPrefix, steps, state, sharedState, createNewStep, onCollectionChanged);
    }

    private static unsafe void DrawSidebar
    (
        string                         idPrefix,
        List<PresetStep>               steps,
        StepTreeEditorState            state,
        StepEditorSharedState          sharedState,
        ExecuteActionRuntimeCursor?    runningCursor,
        Func<PresetStep>               createNewStep,
        Action?                        onCollectionChanged,
        StepTreeExecutionStartOptions? executionStartOptions
    )
    {
        ImGui.TableSetColumnIndex(0);

        if (ImGuiOm.ButtonStretch(state.CurrentStep >= 0 ? "添加动作" : "添加步骤"))
        {
            if (state.CurrentStep < 0)
            {
                steps.Add(createNewStep());
                state.CurrentStep     = steps.Count - 1;
                state.CurrentNodeKind = StepTreeNodeKind.Step;
                state.CurrentAction   = -1;
            }
            else
            {
                var step = steps[state.CurrentStep];
                step.Actions.Add(ExecuteActionBase.CreateDefaultAction(ExecuteActionKind.Wait));
                state.CurrentAction   = step.Actions.Count - 1;
                state.CurrentNodeKind = StepTreeNodeKind.Action;
                state.PendingOpenStep = state.CurrentStep;
                StepEditor.SetActionSelection(state, step, state.CurrentStep, state.CurrentAction);
            }

            onCollectionChanged?.Invoke();
        }

        ImGui.Spacing();
        var filterText = state.FilterText;
        ImGui.SetNextItemWidth(-1);
        if (ImGui.InputTextWithHint($"###StepFilterInput-{idPrefix}", "输入关键字筛选", ref filterText, 256))
            state.FilterText = filterText;

        ImGui.Separator();
        ImGui.Spacing();

        using var child = ImRaii.Child($"{idPrefix}StepTreeSidebarChild", ImGui.GetContentRegionAvail(), true);
        if (!child)
            return;

        var keyword = state.FilterText.Trim();

        for (var stepIndex = 0; stepIndex < steps.Count; stepIndex++)
        {
            var step            = steps[stepIndex];
            var stepRenderState = BuildStepRenderState(step, keyword);
            if (!stepRenderState.Visible)
                continue;

            var isStepSelected      = state.CurrentStep == stepIndex     && state.CurrentNodeKind == StepTreeNodeKind.Step;
            var isStepRunning       = runningCursor is { HasStep: true } && runningCursor.StepIndex == stepIndex;
            var shouldOpenByFilter  = !string.IsNullOrEmpty(keyword);
            var shouldOpenByPending = state.PendingOpenStep == stepIndex;
            if (shouldOpenByFilter || shouldOpenByPending)
                ImGui.SetNextItemOpen(true, ImGuiCond.Always);

            var stepFlags = ImGuiTreeNodeFlags.OpenOnArrow | ImGuiTreeNodeFlags.SpanAvailWidth;
            if (isStepSelected)
                stepFlags |= ImGuiTreeNodeFlags.Selected;

            using var stepHighlightStyle = PushTreeNodeHighlightStyle(isStepSelected, isStepRunning);
            using var stepNode           = ImRaii.TreeNode($"{stepIndex}. {step.Name} ({stepRenderState.ActionCount} 个动作)###{idPrefix}-Step-{stepIndex}", stepFlags);
            var       stepOpen           = stepNode.Success;

            if (ImGui.IsItemClicked(ImGuiMouseButton.Left))
            {
                state.CurrentStep     = stepIndex;
                state.CurrentNodeKind = StepTreeNodeKind.Step;
                state.CurrentAction   = -1;
            }

            using (var dragDropSource = ImRaii.DragDropSource(ImGuiDragDropFlags.None))
            {
                if (dragDropSource)
                {
                    ImGui.SetDragDropPayload($"STEP_REORDER_{idPrefix}", BitConverter.GetBytes(stepIndex));
                    ImGui.Text($"步骤: {stepIndex}. {step.Name}");
                }
            }

            using (var dragDropTarget = ImRaii.DragDropTarget())
            {
                if (dragDropTarget)
                {
                    var payload = ImGui.AcceptDragDropPayload($"STEP_REORDER_{idPrefix}");

                    if (!payload.IsNull && payload.Data != null)
                    {
                        var sourceIndex = *(int*)payload.Data;

                        if (sourceIndex != stepIndex && sourceIndex >= 0 && sourceIndex < steps.Count)
                        {
                            (steps[sourceIndex], steps[stepIndex]) = (steps[stepIndex], steps[sourceIndex]);
                            if (state.CurrentStep == sourceIndex)
                                state.CurrentStep = stepIndex;
                            else if (state.CurrentStep == stepIndex)
                                state.CurrentStep = sourceIndex;

                            onCollectionChanged?.Invoke();
                        }
                    }
                }
            }

            DrawStepContextMenu(idPrefix, steps, state, sharedState, stepIndex, step, createNewStep, onCollectionChanged, executionStartOptions);

            if (shouldOpenByPending)
                state.PendingOpenStep = -1;

            if (!stepOpen)
                continue;

            for (var actionIndex = 0; actionIndex < step.Actions.Count; actionIndex++)
            {
                var action = step.Actions[actionIndex];
                if (!ShouldRenderAction(action, keyword, stepRenderState.ActionMatched))
                    continue;

                var isActionSelected = state.CurrentStep     == stepIndex   &&
                                       state.CurrentAction   == actionIndex &&
                                       state.CurrentNodeKind == StepTreeNodeKind.Action;
                var isActionRunning = runningCursor is { HasAction: true } actionCursor &&
                                      actionCursor.StepIndex   == stepIndex             &&
                                      actionCursor.ActionIndex == actionIndex;

                var actionFlags = ImGuiTreeNodeFlags.Leaf | ImGuiTreeNodeFlags.NoTreePushOnOpen | ImGuiTreeNodeFlags.SpanAvailWidth;
                if (isActionSelected)
                    actionFlags |= ImGuiTreeNodeFlags.Selected;

                using var actionHighlightStyle = PushTreeNodeHighlightStyle(isActionSelected, isActionRunning);
                using var actionNode = ImRaii.TreeNode($"{actionIndex}. {action.Name}###{idPrefix}-Step-{stepIndex}-Action-{actionIndex}", actionFlags);

                if (ImGui.IsItemClicked(ImGuiMouseButton.Left))
                {
                    state.CurrentStep     = stepIndex;
                    state.CurrentAction   = actionIndex;
                    state.CurrentNodeKind = StepTreeNodeKind.Action;
                    StepEditor.SetActionSelection(state, step, stepIndex, actionIndex);
                }

                using (var dragDropSource = ImRaii.DragDropSource(ImGuiDragDropFlags.None))
                {
                    if (dragDropSource)
                    {
                        ImGui.SetDragDropPayload($"{idPrefix}_ACTION_REORDER_{stepIndex}", BitConverter.GetBytes(actionIndex));
                        ImGui.Text($"{actionIndex}. {action.Name}");
                    }
                }

                using (var dragDropTarget = ImRaii.DragDropTarget())
                {
                    if (dragDropTarget)
                    {
                        var payload = ImGui.AcceptDragDropPayload($"{idPrefix}_ACTION_REORDER_{stepIndex}");

                        if (!payload.IsNull && payload.Data != null)
                        {
                            var sourceIndex = *(int*)payload.Data;

                            if (sourceIndex != actionIndex && sourceIndex >= 0 && sourceIndex < step.Actions.Count)
                            {
                                (step.Actions[sourceIndex], step.Actions[actionIndex]) = (step.Actions[actionIndex], step.Actions[sourceIndex]);

                                var selectedIndex = StepEditor.GetActionSelection(state, stepIndex);
                                if (selectedIndex == sourceIndex)
                                    selectedIndex = actionIndex;
                                else if (selectedIndex == actionIndex)
                                    selectedIndex = sourceIndex;
                                StepEditor.SetActionSelection(state, step, stepIndex, selectedIndex);
                                if (state.CurrentStep == stepIndex && state.CurrentNodeKind == StepTreeNodeKind.Action)
                                    state.CurrentAction = selectedIndex;

                                onCollectionChanged?.Invoke();
                            }
                        }
                    }
                }

                StepEditor.DrawActionContextMenu
                (
                    state,
                    step,
                    stepIndex,
                    actionIndex,
                    sharedState,
                    $"{idPrefix}_ActionContentMenu_{stepIndex}_{actionIndex}",
                    executionStartOptions is { IsVisible: true, StartFromAction: not null }
                        ? currentActionIndex => executionStartOptions.StartFromAction(stepIndex, currentActionIndex)
                        : null,
                    onCollectionChanged
                );
                if (state.CurrentStep == stepIndex)
                    state.CurrentAction = StepEditor.NormalizeActionSelection(state, step, stepIndex);
            }
        }

        var blankSize = ImGui.GetContentRegionAvail();
        if (blankSize.X > 0 && blankSize.Y > 0 && ImGui.InvisibleButton($"{idPrefix}BlankClickArea", blankSize))
        {
            state.CurrentStep     = -1;
            state.CurrentAction   = -1;
            state.CurrentNodeKind = StepTreeNodeKind.Step;
        }
    }

    private static void DrawDetails
    (
        string                idPrefix,
        List<PresetStep>      steps,
        StepTreeEditorState   state,
        StepEditorSharedState sharedState,
        Func<PresetStep>      createNewStep,
        Action?               onCollectionChanged
    )
    {
        ImGui.TableSetColumnIndex(1);
        using var detailsChild = ImRaii.Child($"{idPrefix}StepDetailChild", ImGui.GetContentRegionAvail(), true, ImGuiWindowFlags.NoBackground);
        if (!detailsChild)
            return;

        if (state.CurrentStep < 0 || state.CurrentStep >= steps.Count)
        {
            ImGui.TextDisabled("请选择一个步骤进行编辑");
            return;
        }

        var step = steps[state.CurrentStep];
        DrawDetailToolbar(idPrefix, steps, state, sharedState, onCollectionChanged);

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        switch (state.CurrentNodeKind)
        {
            case StepTreeNodeKind.Step:
                DrawStepOverview(step, steps);
                return;
            case StepTreeNodeKind.Action:
            {
                state.CurrentAction = StepEditor.NormalizeActionSelection(state, step, state.CurrentStep);
                var currentAction = state.CurrentAction;

                if (!StepEditor.DrawSelectedActionEditor(state, step, state.CurrentStep, steps, ref currentAction))
                {
                    state.CurrentAction   = -1;
                    state.CurrentNodeKind = StepTreeNodeKind.Step;
                    DrawStepOverview(step, steps);
                    return;
                }

                state.CurrentAction = currentAction;
                return;
            }
            default:
                ImGui.TextDisabled("请选择一个节点进行编辑");
                return;
        }
    }

    private static void DrawStepOverview(PresetStep step, IList<PresetStep> steps)
    {
        if (step.Actions is [ExecutePresetAction])
        {
            StepEditor.DrawSinglePresetActionOverview(step, steps);
            return;
        }

        StepEditor.DrawStepMetadata(step);

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        var actions = step.Actions;
        ImGui.TextColored(KnownColor.LightSkyBlue.ToUInt(), $"动作 (共 {actions.Count} 个)");
        if (actions.Count == 0)
        {
            ImGui.TextDisabled("(无)");
            return;
        }

        foreach (var actionName in actions.Select((action, index) => $"{index}. {action.Name}"))
            ImGui.BulletText(actionName);
    }

    private static void DrawDetailToolbar
    (
        string                idPrefix,
        List<PresetStep>      steps,
        StepTreeEditorState   state,
        StepEditorSharedState sharedState,
        Action?               onCollectionChanged
    )
    {
        if (state.CurrentStep < 0)
            return;

        if (state.CurrentNodeKind == StepTreeNodeKind.Step)
        {
            var stepIndex = state.CurrentStep;

            if (DrawToolbarButton($"{idPrefix}StepMoveUp", FontAwesomeIcon.ArrowUp, "上移"))
            {
                state.CurrentStep = CollectionOperationHelper.Apply(steps, stepIndex, StepOperationType.MoveUp, stepIndex);
                onCollectionChanged?.Invoke();
            }

            ImGui.SameLine();

            if (DrawToolbarButton($"{idPrefix}StepMoveDown", FontAwesomeIcon.ArrowDown, "下移"))
            {
                state.CurrentStep = CollectionOperationHelper.Apply(steps, stepIndex, StepOperationType.MoveDown, stepIndex);
                onCollectionChanged?.Invoke();
            }

            ImGui.SameLine();

            if (DrawToolbarButton($"{idPrefix}StepCopy", FontAwesomeIcon.Copy, "复制步骤"))
                sharedState.StepToCopy = PresetStep.Copy(steps[stepIndex]);

            ImGui.SameLine();

            if (DrawToolbarButton($"{idPrefix}StepDelete", FontAwesomeIcon.Trash, "删除步骤"))
            {
                state.CurrentStep = CollectionOperationHelper.Apply(steps, stepIndex, StepOperationType.Delete, stepIndex);
                NormalizeState(steps, state);
                onCollectionChanged?.Invoke();
            }

            ImGui.SameLine();

            if (DrawToolbarButton($"{idPrefix}StepAddAction", FontAwesomeIcon.Plus, "添加动作"))
            {
                AddActionToCurrentStep(steps, state);
                onCollectionChanged?.Invoke();
            }

            return;
        }

        if (state.CurrentNodeKind == StepTreeNodeKind.Action)
        {
            var step        = steps[state.CurrentStep];
            var actionIndex = state.CurrentAction;
            var actions     = step.Actions;

            if (DrawToolbarButton($"{idPrefix}ActionMoveUp", FontAwesomeIcon.ArrowUp, "上移"))
            {
                MoveSelectedAction(state, step, state.CurrentStep, StepOperationType.MoveUp);
                onCollectionChanged?.Invoke();
            }

            ImGui.SameLine();

            if (DrawToolbarButton($"{idPrefix}ActionMoveDown", FontAwesomeIcon.ArrowDown, "下移"))
            {
                MoveSelectedAction(state, step, state.CurrentStep, StepOperationType.MoveDown);
                onCollectionChanged?.Invoke();
            }

            ImGui.SameLine();

            if (DrawToolbarButton($"{idPrefix}ActionCopy", FontAwesomeIcon.Copy, "复制动作"))
                sharedState.ActionToCopy = ExecuteActionBase.Copy(actions[actionIndex]);

            ImGui.SameLine();

            if (DrawToolbarButton($"{idPrefix}ActionDelete", FontAwesomeIcon.Trash, "删除动作"))
            {
                state.CurrentAction = CollectionOperationHelper.Apply(actions, actionIndex, StepOperationType.Delete, actionIndex);
                StepEditor.SetActionSelection(state, step, state.CurrentStep, state.CurrentAction);
                NormalizeState(steps, state);
                onCollectionChanged?.Invoke();
            }

            ImGui.SameLine();

            if (DrawToolbarButton($"{idPrefix}ActionAddAction", FontAwesomeIcon.Plus, "添加动作"))
            {
                AddActionToCurrentStep(steps, state);
                onCollectionChanged?.Invoke();
            }
        }
    }

    private static bool DrawToolbarButton(string id, FontAwesomeIcon icon, string tooltip) =>
        ImGuiOm.ButtonIcon(id, icon, tooltip, true);

    private static void AddActionToCurrentStep(List<PresetStep> steps, StepTreeEditorState state)
    {
        var step = steps[state.CurrentStep];
        step.Actions.Add(ExecuteActionBase.CreateDefaultAction(ExecuteActionKind.Wait));
        state.CurrentAction   = step.Actions.Count - 1;
        state.CurrentNodeKind = StepTreeNodeKind.Action;
        state.PendingOpenStep = state.CurrentStep;
        StepEditor.SetActionSelection(state, step, state.CurrentStep, state.CurrentAction);
    }

    private static void MoveSelectedAction(StepTreeEditorState state, PresetStep step, int stepIndex, StepOperationType operation)
    {
        var selectedIndex = CollectionOperationHelper.Apply(step.Actions, state.CurrentAction, operation, state.CurrentAction);
        StepEditor.SetActionSelection(state, step, stepIndex, selectedIndex);
        state.CurrentAction = selectedIndex;
    }

    private static void DrawStepContextMenu
    (
        string                         idPrefix,
        List<PresetStep>               steps,
        StepTreeEditorState            state,
        StepEditorSharedState          sharedState,
        int                            index,
        PresetStep                     step,
        Func<PresetStep>               createNewStep,
        Action?                        onCollectionChanged,
        StepTreeExecutionStartOptions? executionStartOptions
    )
    {
        var contextOperation = StepOperationType.Pass;

        if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
            ImGui.OpenPopup($"{idPrefix}_StepContentMenu_{index}");

        using var context = ImRaii.ContextPopupItem($"{idPrefix}_StepContentMenu_{index}");
        if (!context) return;

        ImGui.Text($"第 {index} 步: {step.Name}");
        ImGui.Separator();

        if (executionStartOptions is { IsVisible: true, StartFromStep: not null } && ImGui.MenuItem("从该步骤开始执行"))
            executionStartOptions.StartFromStep(index);

        if (executionStartOptions is { IsVisible: true, StartFromStep: not null })
            ImGui.Separator();

        if (ImGui.MenuItem("复制"))
            sharedState.StepToCopy = PresetStep.Copy(step);

        if (sharedState.StepToCopy != null)
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

        if (index < steps.Count - 1 && ImGui.MenuItem("下移"))
            contextOperation = StepOperationType.MoveDown;

        ImGui.Separator();

        if (ImGui.MenuItem("向上插入新步骤"))
            contextOperation = StepOperationType.InsertUp;

        if (ImGui.MenuItem("向下插入新步骤"))
            contextOperation = StepOperationType.InsertDown;

        ImGui.Separator();

        if (ImGui.MenuItem("复制并插入本步骤"))
            contextOperation = StepOperationType.PasteCurrent;

        ImGui.Separator();

        using var clearMenu = ImRaii.Menu("清空");

        if (clearMenu)
        {
            ImGui.TextDisabled("将清空该步骤下的全部动作");
            ImGui.Separator();

            if (ImGui.MenuItem("确认清空"))
            {
                step.Actions.Clear();

                if (state.CurrentStep == index)
                {
                    state.CurrentAction = -1;
                    if (state.CurrentNodeKind == StepTreeNodeKind.Action)
                        state.CurrentNodeKind = StepTreeNodeKind.Step;
                }

                onCollectionChanged?.Invoke();
            }
        }

        state.CurrentStep = CollectionOperationHelper.Apply
        (
            steps,
            index,
            contextOperation,
            state.CurrentStep,
            createNewStep,
            sharedState.StepToCopy == null ? null : () => PresetStep.Copy(sharedState.StepToCopy),
            () => PresetStep.Copy(step)
        );

        if (contextOperation != StepOperationType.Pass)
            onCollectionChanged?.Invoke();

        NormalizeState(steps, state);
    }

    private static void NormalizeState(List<PresetStep> steps, StepTreeEditorState state)
    {
        if (steps.Count == 0)
        {
            state.CurrentStep     = -1;
            state.CurrentAction   = -1;
            state.CurrentNodeKind = StepTreeNodeKind.Step;
            return;
        }

        if (state.CurrentStep < 0)
            return;

        state.CurrentStep = Math.Clamp(state.CurrentStep, 0, steps.Count - 1);
        var step = steps[state.CurrentStep];
        state.CurrentAction = StepEditor.NormalizeActionSelection(state, step, state.CurrentStep);
        if (state.CurrentNodeKind == StepTreeNodeKind.Action && state.CurrentAction < 0)
            state.CurrentNodeKind = StepTreeNodeKind.Step;
    }

    private static bool ShouldRenderAction(ExecuteActionBase action, string keyword, bool parentMatched)
    {
        if (string.IsNullOrEmpty(keyword) || parentMatched)
            return true;

        return action.Name.Contains(keyword, StringComparison.OrdinalIgnoreCase);
    }

    private static IDisposable? PushTreeNodeHighlightStyle(bool isSelected, bool isRunning)
    {
        if (!isSelected && !isRunning)
            return null;

        var pulse         = (MathF.Sin((float)ImGui.GetTime() * 3.5f) + 1f) * 0.5f;
        var selectedColor = KnownColor.CornflowerBlue.ToVector4() with { W = 0.72f };
        var runningColor  = KnownColor.ForestGreen.ToVector4() with { W = 0.32f + pulse * 0.24f };
        var headerColor   = isSelected && isRunning ? Vector4.Lerp(selectedColor, runningColor, 0.55f) : isSelected ? selectedColor : runningColor;

        var borderColor = isSelected && isRunning
                              ? KnownColor.Gold.ToVector4() with { W = 0.65f + pulse * 0.35f }
                              : isSelected
                                  ? KnownColor.DeepSkyBlue.ToVector4() with { W = 0.9f }
                                  : KnownColor.YellowGreen.ToVector4() with { W = 0.5f + pulse * 0.35f };

        return new TreeNodeHighlightStyle(headerColor, borderColor);
    }

    private sealed class TreeNodeHighlightStyle : IDisposable
    {
        private readonly IDisposable colorStack;

        public TreeNodeHighlightStyle(Vector4 headerColor, Vector4 borderColor)
        {
            colorStack = ImRaii.PushColor(ImGuiCol.Header, headerColor)
                               .Push(ImGuiCol.HeaderHovered, headerColor with { W = Math.Min(1f, headerColor.W + 0.15f) })
                               .Push(ImGuiCol.HeaderActive,  headerColor with { W = Math.Min(1f, headerColor.W + 0.24f) })
                               .Push(ImGuiCol.Border,        borderColor);
            styleStack = ImRaii.PushStyle(ImGuiStyleVar.FrameBorderSize, 1.15f);
        }

        public void Dispose()
        {
            colorStack.Dispose();
            styleStack.Dispose();
        }

        private readonly IDisposable styleStack;
    }

    private static StepRenderState BuildStepRenderState(PresetStep step, string keyword)
    {
        var actionCount = step.Actions.Count;
        if (string.IsNullOrEmpty(keyword))
            return new(true, true, actionCount);

        var stepMatched   = step.Name.Contains(keyword, StringComparison.OrdinalIgnoreCase);
        var actionMatched = step.Actions.Any(action => action.Name.Contains(keyword, StringComparison.OrdinalIgnoreCase));
        return new(stepMatched || actionMatched, stepMatched || actionMatched, actionCount);
    }

    private readonly record struct StepRenderState
    (
        bool Visible,
        bool ActionMatched,
        int  ActionCount
    );

    private static string BuildCurrentPathLabel(List<PresetStep> steps, StepTreeEditorState state)
    {
        if (state.CurrentStep < 0 || state.CurrentStep >= steps.Count)
            return "当前路径";

        var step  = steps[state.CurrentStep];
        var nodes = new List<string> { $"{state.CurrentStep}.{step.Name}" };
        if (state.CurrentNodeKind == StepTreeNodeKind.Action &&
            state.CurrentAction   >= 0                         &&
            state.CurrentAction   < step.Actions.Count)
            nodes.Add($"{state.CurrentAction}.{step.Actions[state.CurrentAction].Name}");

        return string.Join(" > ", nodes);
    }
}
