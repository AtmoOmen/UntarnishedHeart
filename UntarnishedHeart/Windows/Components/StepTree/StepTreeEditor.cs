using System.Numerics;
using UntarnishedHeart.Execution.Common;
using UntarnishedHeart.Execution.Enums;
using UntarnishedHeart.Execution.ExecuteAction;
using UntarnishedHeart.Execution.ExecuteAction.Enums;
using UntarnishedHeart.Execution.Preset;
using UntarnishedHeart.Execution.Preset.Enums;
using UntarnishedHeart.Windows.Helpers;

namespace UntarnishedHeart.Windows.Components;

internal static class StepTreeEditor
{
    public static void Draw
    (
        string                      idPrefix,
        List<PresetStep>            steps,
        StepTreeEditorState         state,
        StepEditorSharedState       sharedState,
        ExecuteActionRuntimeCursor? runningCursor,
        Func<PresetStep>            createNewStep
    )
    {
        NormalizeState(steps, state);
        state.CurrentPathTabLabel = BuildCurrentPathLabel(steps, state);

        using var table = ImRaii.Table($"{idPrefix}StepsTreeTable", 2, ImGuiTableFlags.Resizable | ImGuiTableFlags.BordersInnerV);
        if (!table) return;

        ImGui.TableSetupColumn($"{idPrefix}Sidebar", ImGuiTableColumnFlags.WidthFixed, 280f * GlobalUIScale);
        ImGui.TableSetupColumn($"{idPrefix}Detail",  ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableNextRow();

        DrawSidebar(idPrefix, steps, state, sharedState, runningCursor, createNewStep);
        DrawDetails(idPrefix, steps, state);
    }

    private static unsafe void DrawSidebar
    (
        string                      idPrefix,
        List<PresetStep>            steps,
        StepTreeEditorState         state,
        StepEditorSharedState       sharedState,
        ExecuteActionRuntimeCursor? runningCursor,
        Func<PresetStep>            createNewStep
    )
    {
        ImGui.TableSetColumnIndex(0);

        if (ImGuiOm.ButtonStretch("添加步骤"))
        {
            steps.Add(createNewStep());
            state.CurrentStep     = steps.Count - 1;
            state.CurrentNodeKind = StepTreeNodeKind.Step;
            state.CurrentAction   = -1;
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

            var isStepSelected      = state.CurrentStep == stepIndex     && state.CurrentNodeKind   == StepTreeNodeKind.Step;
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
                        }
                    }
                }
            }

            DrawStepContextMenu(idPrefix, steps, state, sharedState, stepIndex, step, createNewStep);

            if (!stepOpen)
                continue;

            DrawPhaseNode(idPrefix, step, stepIndex, PresetStepPhase.Enter, "进入阶段", stepRenderState.EnterMatched, state, sharedState, runningCursor, keyword);
            DrawPhaseNode(idPrefix, step, stepIndex, PresetStepPhase.Body,  "进行阶段", stepRenderState.BodyMatched,  state, sharedState, runningCursor, keyword);
            DrawPhaseNode(idPrefix, step, stepIndex, PresetStepPhase.Exit,  "离开阶段", stepRenderState.ExitMatched,  state, sharedState, runningCursor, keyword);

            if (shouldOpenByPending && state.PendingOpenPhase == null)
                state.PendingOpenStep = -1;
        }
    }

    private static unsafe void DrawPhaseNode
    (
        string                      idPrefix,
        PresetStep                  step,
        int                         stepIndex,
        PresetStepPhase             phase,
        string                      phaseName,
        bool                        phaseMatched,
        StepTreeEditorState         state,
        StepEditorSharedState       sharedState,
        ExecuteActionRuntimeCursor? runningCursor,
        string                      keyword
    )
    {
        var actions             = StepEditor.GetPhaseActions(step, phase);
        var shouldOpenByFilter  = !string.IsNullOrEmpty(keyword)     && phaseMatched;
        var shouldOpenByPending = state.PendingOpenStep == stepIndex && state.PendingOpenPhase == phase;
        if (shouldOpenByFilter || shouldOpenByPending)
            ImGui.SetNextItemOpen(true, ImGuiCond.Always);

        var isPhaseSelected = state.CurrentStep == stepIndex             && state.CurrentPhase == phase     && state.CurrentNodeKind == StepTreeNodeKind.Phase;
        var isPhaseRunning  = runningCursor is { HasPhase: true } cursor && cursor.StepIndex   == stepIndex && cursor.Phase          == phase;

        var phaseFlags = ImGuiTreeNodeFlags.OpenOnArrow | ImGuiTreeNodeFlags.SpanAvailWidth;
        if (isPhaseSelected)
            phaseFlags |= ImGuiTreeNodeFlags.Selected;

        using var phaseHighlightStyle = PushTreeNodeHighlightStyle(isPhaseSelected, isPhaseRunning);
        using var phaseNode           = ImRaii.TreeNode($"{phaseName} ({actions.Count} 个动作)###{idPrefix}-Step-{stepIndex}-Phase-{phase}", phaseFlags);
        var       phaseOpen           = phaseNode.Success;

        if (ImGui.IsItemClicked(ImGuiMouseButton.Left))
        {
            state.CurrentStep     = stepIndex;
            state.CurrentPhase    = phase;
            state.CurrentNodeKind = StepTreeNodeKind.Phase;
            state.CurrentAction   = StepEditor.NormalizeActionSelection(step, phase);
        }

        DrawPhaseContextMenu(idPrefix, step, stepIndex, phase, state);

        if (shouldOpenByPending)
        {
            state.PendingOpenStep  = -1;
            state.PendingOpenPhase = null;
        }

        if (!phaseOpen)
            return;

        for (var actionIndex = 0; actionIndex < actions.Count; actionIndex++)
        {
            var action = actions[actionIndex];
            if (!ShouldRenderAction(action, keyword, phaseMatched))
                continue;

            var isActionSelected = state.CurrentStep     == stepIndex   &&
                                   state.CurrentPhase    == phase       &&
                                   state.CurrentAction   == actionIndex &&
                                   state.CurrentNodeKind == StepTreeNodeKind.Action;
            var isActionRunning = runningCursor is { HasAction: true } actionCursor &&
                                  actionCursor.StepIndex   == stepIndex             &&
                                  actionCursor.Phase       == phase                 &&
                                  actionCursor.ActionIndex == actionIndex;

            var actionFlags = ImGuiTreeNodeFlags.Leaf | ImGuiTreeNodeFlags.NoTreePushOnOpen | ImGuiTreeNodeFlags.SpanAvailWidth;
            if (isActionSelected)
                actionFlags |= ImGuiTreeNodeFlags.Selected;

            using var actionHighlightStyle = PushTreeNodeHighlightStyle(isActionSelected, isActionRunning);
            using var actionNode = ImRaii.TreeNode($"{actionIndex}. {action.Name}###{idPrefix}-Step-{stepIndex}-Phase-{phase}-Action-{actionIndex}", actionFlags);

            if (ImGui.IsItemClicked(ImGuiMouseButton.Left))
            {
                state.CurrentStep     = stepIndex;
                state.CurrentPhase    = phase;
                state.CurrentAction   = actionIndex;
                state.CurrentNodeKind = StepTreeNodeKind.Action;
                StepEditor.SetActionSelection(step, phase, actionIndex);
            }

            using (var dragDropSource = ImRaii.DragDropSource(ImGuiDragDropFlags.None))
            {
                if (dragDropSource)
                {
                    ImGui.SetDragDropPayload($"{idPrefix}_ACTION_REORDER_{stepIndex}_{phase}", BitConverter.GetBytes(actionIndex));
                    ImGui.Text($"{actionIndex}. {action.Name}");
                }
            }

            using (var dragDropTarget = ImRaii.DragDropTarget())
            {
                if (dragDropTarget)
                {
                    var payload = ImGui.AcceptDragDropPayload($"{idPrefix}_ACTION_REORDER_{stepIndex}_{phase}");

                    if (!payload.IsNull && payload.Data != null)
                    {
                        var sourceIndex = *(int*)payload.Data;

                        if (sourceIndex != actionIndex && sourceIndex >= 0 && sourceIndex < actions.Count)
                        {
                            (actions[sourceIndex], actions[actionIndex]) = (actions[actionIndex], actions[sourceIndex]);

                            var selectedIndex = StepEditor.GetActionSelection(step, phase);
                            if (selectedIndex == sourceIndex)
                                selectedIndex = actionIndex;
                            else if (selectedIndex == actionIndex)
                                selectedIndex = sourceIndex;
                            StepEditor.SetActionSelection(step, phase, selectedIndex);
                            if (state.CurrentStep == stepIndex && state.CurrentPhase == phase && state.CurrentNodeKind == StepTreeNodeKind.Action)
                                state.CurrentAction = selectedIndex;
                        }
                    }
                }
            }

            StepEditor.DrawActionContextMenu(step, phase, actionIndex, sharedState, $"{idPrefix}_ActionContentMenu_{stepIndex}_{phase}_{actionIndex}");
            if (state.CurrentStep == stepIndex && state.CurrentPhase == phase)
                state.CurrentAction = StepEditor.NormalizeActionSelection(step, phase);
        }

    }

    private static void DrawDetails(string idPrefix, List<PresetStep> steps, StepTreeEditorState state)
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

        switch (state.CurrentNodeKind)
        {
            case StepTreeNodeKind.Step:
                DrawStepOverview(step);
                return;
            case StepTreeNodeKind.Phase:
                DrawPhaseOverview(step, state.CurrentPhase);
                return;
            case StepTreeNodeKind.Action:
            {
                state.CurrentAction = StepEditor.NormalizeActionSelection(step, state.CurrentPhase);
                var currentAction = state.CurrentAction;

                if (!StepEditor.DrawSelectedActionEditor(step, state.CurrentPhase, ref currentAction))
                {
                    state.CurrentAction   = -1;
                    state.CurrentNodeKind = StepTreeNodeKind.Phase;
                    DrawPhaseOverview(step, state.CurrentPhase);
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

    private static void DrawStepOverview(PresetStep step)
    {
        StepEditor.DrawStepMetadata(step);

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        DrawPhaseSummary(step, PresetStepPhase.Enter);
        DrawPhaseSummary(step, PresetStepPhase.Body);
        DrawPhaseSummary(step, PresetStepPhase.Exit);
    }

    private static void DrawPhaseOverview(PresetStep step, PresetStepPhase phase)
    {
        var actions = StepEditor.GetPhaseActions(step, phase);
        ImGui.TextColored(KnownColor.LightSkyBlue.ToUInt(), $"{GetPhaseName(phase)}");
        ImGui.TextDisabled($"共 {actions.Count} 个动作");
        ImGui.Spacing();

        if (actions.Count == 0)
        {
            ImGui.TextDisabled("该阶段暂无动作");
            return;
        }

        foreach (var (action, index) in actions.Select(static (value, i) => (value, i)))
        {
            var condition = action.Condition;
            ImGui.TextUnformatted($"{index}. {action.Name}");
            ImGui.BulletText($"处理类型: {condition.ExecuteType.GetDescription()}");
            ImGui.BulletText($"条件关系: {condition.RelationType.GetDescription()}");
            ImGui.BulletText($"条件数量: {condition.Conditions.Count}");
            ImGui.BulletText($"执行次数: 最少 {condition.MinExecuteCount} / 最大 {(condition.MaxExecuteCount <= 0 ? "无限" : condition.MaxExecuteCount.ToString())}");
            if (condition.IntervalMs > 0)
                ImGui.BulletText($"重复间隔: {condition.IntervalMs} ms");
            ImGui.Spacing();
        }
    }

    private static void DrawPhaseSummary(PresetStep step, PresetStepPhase phase)
    {
        var actions = StepEditor.GetPhaseActions(step, phase);
        ImGui.TextColored(KnownColor.LightSkyBlue.ToUInt(), $"{GetPhaseName(phase)}");

        ImGui.SameLine();
        ImGui.TextDisabled($"(共 {actions.Count} 个动作)");

        if (actions.Count == 0)
        {
            ImGui.TextDisabled("(无)");
            ImGui.Spacing();
            return;
        }

        foreach (var actionName in actions.Select((action, index) => $"{index}. {action.Name}"))
            ImGui.BulletText(actionName);

        ImGui.Spacing();
    }

    private static void DrawStepContextMenu
    (
        string                idPrefix,
        List<PresetStep>      steps,
        StepTreeEditorState   state,
        StepEditorSharedState sharedState,
        int                   index,
        PresetStep            step,
        Func<PresetStep>      createNewStep
    )
    {
        var contextOperation = StepOperationType.Pass;

        if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
            ImGui.OpenPopup($"{idPrefix}_StepContentMenu_{index}");

        using var context = ImRaii.ContextPopupItem($"{idPrefix}_StepContentMenu_{index}");
        if (!context) return;

        ImGui.Text($"第 {index} 步: {step.Name}");
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

        using var clearMenu = ImRaii.Menu("清空");

        if (clearMenu)
        {
            ImGui.TextDisabled("将清空该步骤下的全部动作");
            ImGui.Separator();

            if (ImGui.MenuItem("确认清空"))
            {
                step.EnterActions.Clear();
                step.BodyActions.Clear();
                step.ExitActions.Clear();

                if (state.CurrentStep == index)
                {
                    state.CurrentAction = -1;
                    if (state.CurrentNodeKind == StepTreeNodeKind.Action)
                        state.CurrentNodeKind = StepTreeNodeKind.Phase;
                }
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

        NormalizeState(steps, state);
    }

    private static void DrawPhaseContextMenu(string idPrefix, PresetStep step, int stepIndex, PresetStepPhase phase, StepTreeEditorState state)
    {
        if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
            ImGui.OpenPopup($"{idPrefix}_PhaseMenu_{stepIndex}_{phase}");

        using var context = ImRaii.ContextPopupItem($"{idPrefix}_PhaseMenu_{stepIndex}_{phase}");
        if (!context)
            return;

        var actions = StepEditor.GetPhaseActions(step, phase);

        if (ImGui.MenuItem("添加动作"))
        {
            actions.Add(ExecuteActionBase.CreateDefaultAction(ExecuteActionKind.Wait));
            var newIndex = actions.Count - 1;
            StepEditor.SetActionSelection(step, phase, newIndex);

            state.CurrentStep      = stepIndex;
            state.CurrentPhase     = phase;
            state.CurrentAction    = newIndex;
            state.CurrentNodeKind  = StepTreeNodeKind.Action;
            state.PendingOpenStep  = stepIndex;
            state.PendingOpenPhase = phase;
        }

        using var clearMenu = ImRaii.Menu("清空");
        if (!clearMenu)
            return;

        ImGui.TextDisabled($"将清空 {GetPhaseName(phase)} 的全部动作");
        ImGui.Separator();

        if (ImGui.MenuItem("确认清空"))
        {
            actions.Clear();
            StepEditor.SetActionSelection(step, phase, -1);

            if (state.CurrentStep == stepIndex && state.CurrentPhase == phase)
            {
                state.CurrentAction   = -1;
                state.CurrentNodeKind = StepTreeNodeKind.Phase;
            }
        }
    }

    private static void NormalizeState(List<PresetStep> steps, StepTreeEditorState state)
    {
        state.CurrentStep = steps.Count == 0 ? -1 : Math.Clamp(state.CurrentStep, 0, steps.Count - 1);

        if (state.CurrentStep < 0)
        {
            state.CurrentAction   = -1;
            state.CurrentNodeKind = StepTreeNodeKind.Step;
            return;
        }

        var step = steps[state.CurrentStep];
        state.CurrentAction = StepEditor.NormalizeActionSelection(step, state.CurrentPhase);
        if (state.CurrentNodeKind == StepTreeNodeKind.Action && state.CurrentAction < 0)
            state.CurrentNodeKind = StepTreeNodeKind.Phase;
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

    private static string GetPhaseName(PresetStepPhase phase) =>
        phase switch
        {
            PresetStepPhase.Enter => "进入阶段",
            PresetStepPhase.Body  => "进行阶段",
            PresetStepPhase.Exit  => "离开阶段",
            _                     => "未知阶段"
        };

    private static StepRenderState BuildStepRenderState(PresetStep step, string keyword)
    {
        var actionCount = step.EnterActions.Count + step.BodyActions.Count + step.ExitActions.Count;
        if (string.IsNullOrEmpty(keyword))
            return new(true, true, true, true, actionCount);

        var stepMatched  = step.Name.Contains(keyword, StringComparison.OrdinalIgnoreCase);
        var enterMatched = stepMatched || step.EnterActions.Any(action => action.Name.Contains(keyword, StringComparison.OrdinalIgnoreCase));
        var bodyMatched  = stepMatched || step.BodyActions.Any(action => action.Name.Contains(keyword,  StringComparison.OrdinalIgnoreCase));
        var exitMatched  = stepMatched || step.ExitActions.Any(action => action.Name.Contains(keyword,  StringComparison.OrdinalIgnoreCase));
        var visible      = stepMatched || enterMatched || bodyMatched || exitMatched;
        return new(visible, enterMatched, bodyMatched, exitMatched, actionCount);
    }

    private readonly record struct StepRenderState
    (
        bool Visible,
        bool EnterMatched,
        bool BodyMatched,
        bool ExitMatched,
        int  ActionCount
    );

    private static string BuildCurrentPathLabel(List<PresetStep> steps, StepTreeEditorState state)
    {
        if (state.CurrentStep < 0 || state.CurrentStep >= steps.Count)
            return "当前路径";

        var step  = steps[state.CurrentStep];
        var nodes = new List<string> { $"{state.CurrentStep}.{step.Name}" };
        if (state.CurrentNodeKind is StepTreeNodeKind.Phase or StepTreeNodeKind.Action)
            nodes.Add(GetPhaseName(state.CurrentPhase));

        if (state.CurrentNodeKind == StepTreeNodeKind.Action)
        {
            var actions = StepEditor.GetPhaseActions(step, state.CurrentPhase);
            if (state.CurrentAction >= 0 && state.CurrentAction < actions.Count)
                nodes.Add($"{state.CurrentAction}.{actions[state.CurrentAction].Name}");
        }

        return string.Join(" > ", nodes);
    }
}
