using OmenTools.Interop.Game.Helpers;
using OmenTools.OmenService;
using UntarnishedHeart.Execution.Condition;
using UntarnishedHeart.Execution.Condition.Enums;
using UntarnishedHeart.Execution.ExecuteAction;
using UntarnishedHeart.Execution.ExecuteAction.Enums;
using UntarnishedHeart.Execution.ExecuteAction.Implementations;
using UntarnishedHeart.Execution.ExecuteAction.Models;
using UntarnishedHeart.Execution.Preset;
using UntarnishedHeart.Execution.Preset.Enums;
using UntarnishedHeart.Execution.Preset.Helpers;

namespace UntarnishedHeart.Execution.Common;

public abstract class ExecuteActionExecutionHost
{
    private volatile ExecuteActionRuntimeCursor runtimeCursor = ExecuteActionRuntimeCursor.Empty;

    protected enum ActionFlowKind
    {
        Continue,
        JumpToStep,
        RestartCurrentStep,
        JumpToAction,
        RestartCurrentAction,
        LeaveAndEnd,
        LeaveAndRestart
    }

    protected readonly record struct ActionFlowResult
    (
        ActionFlowKind Kind,
        int            Index = -1
    )
    {
        public static ActionFlowResult Continue() => new(ActionFlowKind.Continue);

        public static ActionFlowResult JumpToStep(int stepIndex) => new(ActionFlowKind.JumpToStep, stepIndex);

        public static ActionFlowResult RestartStep() => new(ActionFlowKind.RestartCurrentStep);

        public static ActionFlowResult JumpToAction(int actionIndex) => new(ActionFlowKind.JumpToAction, actionIndex);

        public static ActionFlowResult RestartAction() => new(ActionFlowKind.RestartCurrentAction);

        public static ActionFlowResult LeaveAndEnd() => new(ActionFlowKind.LeaveAndEnd);

        public static ActionFlowResult LeaveAndRestart() => new(ActionFlowKind.LeaveAndRestart);
    }

    protected ExecuteActionRuntimeCursor CurrentRuntimeCursor => runtimeCursor;

    protected void ResetRuntimeCursor() => runtimeCursor = ExecuteActionRuntimeCursor.Empty;

    protected void SetRuntimeCursor(int stepIndex, PresetStepPhase? phase = null, int actionIndex = -1) =>
        runtimeCursor = new(stepIndex, phase, actionIndex);

    protected Task<ActionFlowResult> ExecuteStepAsync(PresetStep step, int stepIndex, CancellationToken cancellationToken) =>
        ExecuteStepAsync(step, stepIndex, null, cancellationToken);

    protected async Task<ActionFlowResult> ExecuteStepAsync
    (
        PresetStep                  step,
        int                         stepIndex,
        ExecuteActionRuntimeCursor? startCursor,
        CancellationToken           cancellationToken
    )
    {
        SetRuntimeCursor(stepIndex);
        var startPhase       = startCursor is { HasPhase: true, StepIndex: var cursorStepIndex } && cursorStepIndex == stepIndex ? startCursor.Phase : null;
        var hasReachedPhase  = !startPhase.HasValue;

        foreach (var phase in Enum.GetValues<PresetStepPhase>())
        {
            if (!hasReachedPhase)
            {
                if (phase != startPhase)
                    continue;

                hasReachedPhase = true;
            }

            var startActionIndex = startCursor is { HasAction: true, StepIndex: var actionStepIndex } &&
                                   actionStepIndex == stepIndex                                  &&
                                   startCursor.Phase == phase
                                       ? startCursor.ActionIndex
                                       : 0;

            SetRuntimeCursor(stepIndex, phase);
            var actions     = GetActions(step, phase);
            var phaseResult = await ExecutePhaseAsync(stepIndex, step, phase, actions, cancellationToken, startActionIndex);
            if (phaseResult.Kind != ActionFlowKind.Continue)
                return phaseResult;
        }

        return ActionFlowResult.Continue();
    }

    protected async Task<ActionFlowResult> ExecutePhaseAsync
    (
        int                     stepIndex,
        PresetStep              step,
        PresetStepPhase         phase,
        List<ExecuteActionBase> actions,
        CancellationToken       cancellationToken,
        int                     startActionIndex = 0
    )
    {
        if (startActionIndex < 0)
            throw new InvalidOperationException($"无效的执行动作索引: {startActionIndex}");

        if (startActionIndex > 0)
            ValidateActionIndex(startActionIndex, actions.Count);

        for (var actionIndex = startActionIndex; actionIndex < actions.Count;)
        {
            SetRuntimeCursor(stepIndex, phase, actionIndex);
            var action = actions[actionIndex];
            var result = await ExecuteActionAsync(stepIndex, step, phase, actionIndex, action, actions.Count, cancellationToken);

            switch (result.Kind)
            {
                case ActionFlowKind.Continue:
                    actionIndex++;
                    break;
                case ActionFlowKind.JumpToAction:
                    actionIndex = result.Index;
                    break;
                case ActionFlowKind.RestartCurrentAction:
                    break;
                case ActionFlowKind.JumpToStep:
                case ActionFlowKind.RestartCurrentStep:
                case ActionFlowKind.LeaveAndEnd:
                case ActionFlowKind.LeaveAndRestart:
                    return result;
                default:
                    throw new InvalidOperationException($"不支持的动作跳转结果: {result.Kind}");
            }
        }

        return ActionFlowResult.Continue();
    }

    protected async Task<ActionFlowResult> ExecuteActionAsync
    (
        int               stepIndex,
        PresetStep        step,
        PresetStepPhase   phase,
        int               actionIndex,
        ExecuteActionBase action,
        int               currentPhaseActionCount,
        CancellationToken cancellationToken
    )
    {
        var conditionCollection = action.Condition ?? new ConditionCollection();
        var executedCount       = 0;

        switch (conditionCollection.ExecuteType)
        {
            case ConditionExecuteType.Wait:
                await WaitUntilAsync
                (
                    () => conditionCollection.Evaluate(CreateConditionContext()),
                    BuildActionMessage(stepIndex, step, phase, actionIndex, "等待条件满足"),
                    cancellationToken
                );
                return await ExecuteActionCoreAsync(stepIndex, step, phase, actionIndex, action, currentPhaseActionCount, cancellationToken);

            case ConditionExecuteType.Skip:
                if (!conditionCollection.Evaluate(CreateConditionContext()))
                    return ActionFlowResult.Continue();

                return await ExecuteActionCoreAsync(stepIndex, step, phase, actionIndex, action, currentPhaseActionCount, cancellationToken);

            case ConditionExecuteType.Repeat:
                while (ShouldRepeat(conditionCollection, executedCount))
                {
                    var result = await ExecuteActionCoreAsync(stepIndex, step, phase, actionIndex, action, currentPhaseActionCount, cancellationToken);
                    executedCount++;
                    if (result.Kind != ActionFlowKind.Continue)
                        return result;

                    if (ShouldRepeat(conditionCollection, executedCount) && conditionCollection.IntervalMs > 0)
                        await DelayAsync(conditionCollection.IntervalMs, BuildActionMessage(stepIndex, step, phase, actionIndex, "等待重复间隔"), cancellationToken);
                }

                return ActionFlowResult.Continue();

            case ConditionExecuteType.Sustain:
                while (ShouldSustain(conditionCollection, executedCount))
                {
                    var result = await ExecuteActionCoreAsync(stepIndex, step, phase, actionIndex, action, currentPhaseActionCount, cancellationToken);
                    executedCount++;
                    if (result.Kind != ActionFlowKind.Continue)
                        return result;

                    if (ShouldSustain(conditionCollection, executedCount) && conditionCollection.IntervalMs > 0)
                        await DelayAsync(conditionCollection.IntervalMs, BuildActionMessage(stepIndex, step, phase, actionIndex, "等待持续间隔"), cancellationToken);
                }

                return ActionFlowResult.Continue();

            default:
                throw new InvalidOperationException($"不支持的条件执行类型: {conditionCollection.ExecuteType}");
        }
    }

    protected virtual async Task<ActionFlowResult> ExecuteActionCoreAsync
    (
        int               stepIndex,
        PresetStep        step,
        PresetStepPhase   phase,
        int               actionIndex,
        ExecuteActionBase action,
        int               currentPhaseActionCount,
        CancellationToken cancellationToken
    )
    {
        var actionLabel = BuildActionMessage(stepIndex, step, phase, actionIndex, action.Name);

        switch (action)
        {
            case WaitMillisecondsAction waitMilliseconds:
                if (waitMilliseconds.Milliseconds > 0)
                    await DelayAsync(waitMilliseconds.Milliseconds, actionLabel, cancellationToken);
                return ActionFlowResult.Continue();

            case JumpToStepAction jumpToStep:
                ValidateStepIndex(jumpToStep.StepIndex);
                SetRunningMessage(actionLabel);
                return ActionFlowResult.JumpToStep(jumpToStep.StepIndex);

            case RestartCurrentStepAction:
                SetRunningMessage(actionLabel);
                return ActionFlowResult.RestartStep();

            case JumpToActionAction jumpToAction:
                ValidateActionIndex(jumpToAction.ActionIndex, currentPhaseActionCount);
                SetRunningMessage(actionLabel);
                return ActionFlowResult.JumpToAction(jumpToAction.ActionIndex);

            case RestartCurrentActionAction:
                SetRunningMessage(actionLabel);
                return ActionFlowResult.RestartAction();

            case LeaveDutyAndEndAction:
                SetRunningMessage(actionLabel);
                LeaveDuty();
                return ActionFlowResult.LeaveAndEnd();

            case LeaveDutyAndRestartAction:
                await LeaveDutyAndRestartAsync(actionLabel, cancellationToken);
                return ActionFlowResult.LeaveAndRestart();

            case TextCommandAction textCommand:
                await RunCommandsAsync(textCommand.Commands, actionLabel, cancellationToken);
                return ActionFlowResult.Continue();

            case GameCommandAction gameCommand:
                SetRunningMessage(actionLabel);
                ExecuteCommandManager.Instance().ExecuteCommand(gameCommand.Command, gameCommand.Param1, gameCommand.Param2, gameCommand.Param3, gameCommand.Param4);
                return ActionFlowResult.Continue();

            case GameCommandComplexAction gameCommandComplex:
                SetRunningMessage(actionLabel);

                if (gameCommandComplex.UseLocation)
                    ExecuteCommandManager.Instance().ExecuteCommandComplexLocation
                    (
                        gameCommandComplex.Command,
                        gameCommandComplex.Location,
                        gameCommandComplex.Param1,
                        gameCommandComplex.Param2,
                        gameCommandComplex.Param3,
                        gameCommandComplex.Param4
                    );
                else
                    ExecuteCommandManager.Instance().ExecuteCommandComplex
                    (
                        gameCommandComplex.Command,
                        gameCommandComplex.Target,
                        gameCommandComplex.Param1,
                        gameCommandComplex.Param2,
                        gameCommandComplex.Param3,
                        gameCommandComplex.Param4
                    );

                return ActionFlowResult.Continue();

            case SelectTargetAction selectTarget:
                SetRunningMessage(actionLabel);
                PresetTargetResolver.SelectTarget(PresetTargetResolver.Resolve(selectTarget.Selector));
                return ActionFlowResult.Continue();

            case InteractTargetAction interactTarget:
            {
                SetRunningMessage(actionLabel);
                var gameObject = PresetTargetResolver.Resolve(interactTarget.Selector);
                if (gameObject == null)
                    return ActionFlowResult.Continue();

                gameObject.TargetInteract();
                if (interactTarget.OpenObjectInteraction)
                    PresetTargetResolver.OpenObjectInteraction(gameObject);

                return ActionFlowResult.Continue();
            }

            case InteractNearestObjectAction:
                await ExecuteNearestInteractAsync(actionLabel, cancellationToken);
                return ActionFlowResult.Continue();

            case UseActionExecuteAction useAction:
            {
                SetRunningMessage(actionLabel);
                var targetID = PresetTargetResolver.Resolve(useAction.TargetSelector)?.GameObjectID ?? 0xE000_0000UL;

                if (useAction.UseLocation)
                    UseActionManager.Instance().UseActionLocation(useAction.Action.ActionType, useAction.Action.ActionID, targetID, useAction.Location);
                else
                    UseActionManager.Instance().UseAction(useAction.Action.ActionType, useAction.Action.ActionID, targetID);

                return ActionFlowResult.Continue();
            }

            case MoveToPositionAction moveToPosition:
                await ExecuteMovementActionAsync(moveToPosition, actionLabel, cancellationToken);
                return ActionFlowResult.Continue();

            case SwitchClassJobAction switchClassJob:
            {
                SetRunningMessage(actionLabel);

                switch (switchClassJob.Mode)
                {
                    case SwitchClassJobMode.ByClassJob:
                        if (switchClassJob.JobID == 0)
                            throw new InvalidOperationException("切换职业动作缺少目标职业");

                        if (!LocalPlayerState.SwitchGearset(switchClassJob.JobID))
                            throw new InvalidOperationException($"切换职业失败: {switchClassJob.JobID}");

                        return ActionFlowResult.Continue();

                    case SwitchClassJobMode.ByGearsetID:
                        if (switchClassJob.GearsetID is < 0 or > 99)
                            throw new InvalidOperationException($"切换职业动作的套装编号无效: {switchClassJob.GearsetID}");

                        if (!LocalPlayerState.SwitchGearset((byte)switchClassJob.GearsetID))
                            throw new InvalidOperationException($"切换套装失败: {switchClassJob.GearsetID}");

                        return ActionFlowResult.Continue();

                    default:
                        throw new InvalidOperationException($"不支持的切换职业方式: {switchClassJob.Mode}");
                }
            }

            case AddonCallbackAction addonCallback:
            {
                SetRunningMessage(actionLabel);

                unsafe
                {
                    if (!AddonHelper.TryGetByName(addonCallback.AddonName, out var addon))
                        return ActionFlowResult.Continue();

                    using var atkValues = AtkValueParameter.CreateValueArray(addonCallback.Parameters);
                    addon->Callback(atkValues);
                }

                return ActionFlowResult.Continue();
            }

            case AgentReceiveEventAction agentReceiveEvent:
            {
                SetRunningMessage(actionLabel);

                unsafe
                {
                    using var atkValues = AtkValueParameter.CreateValueArray(agentReceiveEvent.Parameters);
                    agentReceiveEvent.AgentID.SendEvent(agentReceiveEvent.EventKind, atkValues);
                }

                return ActionFlowResult.Continue();
            }

            default:
            {
                var customResult = await ExecuteCustomActionCoreAsync
                                   (
                                       stepIndex,
                                       step,
                                       phase,
                                       actionIndex,
                                       action,
                                       currentPhaseActionCount,
                                       actionLabel,
                                       cancellationToken
                                   );
                if (customResult is { } result)
                    return result;

                throw new InvalidOperationException($"不支持的执行动作类型: {action.Kind}");
            }
        }
    }

    protected static List<ExecuteActionBase> GetActions(PresetStep step, PresetStepPhase phase) =>
        phase switch
        {
            PresetStepPhase.Enter => step.EnterActions,
            PresetStepPhase.Body  => step.BodyActions,
            PresetStepPhase.Exit  => step.ExitActions,
            _                     => throw new InvalidOperationException($"不支持的阶段: {phase}")
        };

    protected static string BuildActionMessage(int stepIndex, PresetStep step, PresetStepPhase phase, int actionIndex, string suffix) =>
        $"步骤 {stepIndex}: {step.Name} / {phase.GetDescription()} / 动作 {actionIndex}: {suffix}";

    protected bool ShouldRepeat(ConditionCollection conditionCollection, int executedCount)
    {
        if (conditionCollection.MaxExecuteCount > 0 && executedCount >= conditionCollection.MaxExecuteCount)
            return false;

        if (executedCount < conditionCollection.MinExecuteCount)
            return true;

        return !conditionCollection.Evaluate(CreateConditionContext());
    }

    protected bool ShouldSustain(ConditionCollection conditionCollection, int executedCount)
    {
        if (conditionCollection.MaxExecuteCount > 0 && executedCount >= conditionCollection.MaxExecuteCount)
            return false;

        if (executedCount < conditionCollection.MinExecuteCount)
            return true;

        return conditionCollection.Evaluate(CreateConditionContext());
    }

    protected abstract ConditionContext CreateConditionContext();

    protected abstract void SetRunningMessage(string message);

    protected abstract void ValidateStepIndex(int stepIndex);

    protected abstract void ValidateActionIndex(int actionIndex, int currentPhaseActionCount);

    protected abstract void LeaveDuty();

    protected abstract Task LeaveDutyAndRestartAsync(string message, CancellationToken cancellationToken);

    protected abstract Task RunCommandsAsync(string commands, string actionLabel, CancellationToken cancellationToken);

    protected abstract Task ExecuteNearestInteractAsync(string sourceName, CancellationToken cancellationToken);

    protected abstract Task ExecuteMovementActionAsync(MoveToPositionAction action, string actionLabel, CancellationToken cancellationToken);

    protected abstract Task WaitUntilAsync(Func<bool> predicate, string message, CancellationToken cancellationToken, int intervalMs = 100);

    protected abstract Task DelayAsync(int delayMs, string message, CancellationToken cancellationToken);

    protected virtual Task<ActionFlowResult?> ExecuteCustomActionCoreAsync
    (
        int               stepIndex,
        PresetStep        step,
        PresetStepPhase   phase,
        int               actionIndex,
        ExecuteActionBase action,
        int               currentPhaseActionCount,
        string            actionLabel,
        CancellationToken cancellationToken
    ) =>
        Task.FromResult<ActionFlowResult?>(null);
}
