using FFXIVClientStructs.FFXIV.Client.UI;
using OmenTools.Interop.Game.Helpers;
using OmenTools.OmenService;
using UntarnishedHeart.Execution.Condition;
using UntarnishedHeart.Execution.Condition.Enums;
using UntarnishedHeart.Execution.Enums;
using UntarnishedHeart.Execution.ExecuteAction;
using UntarnishedHeart.Execution.ExecuteAction.Enums;
using UntarnishedHeart.Execution.ExecuteAction.Implementations;
using UntarnishedHeart.Execution.ExecuteAction.Models;
using UntarnishedHeart.Execution.Preset;
using UntarnishedHeart.Execution.Preset.Helpers;

namespace UntarnishedHeart.Execution.Common;

public abstract class ExecuteActionExecutionHost
{
    private volatile ExecuteActionRuntimeCursor runtimeCursor = ExecuteActionRuntimeCursor.Empty;

    internal MovementController Movement { get; } = new();

    protected enum ActionFlowKind
    {
        Continue,
        JumpToStep,
        JumpToAction,
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

        public static ActionFlowResult JumpToStep
        (
            int stepIndex
        ) => new(ActionFlowKind.JumpToStep, stepIndex);

        public static ActionFlowResult JumpToAction
        (
            int actionIndex
        ) => new(ActionFlowKind.JumpToAction, actionIndex);

        public static ActionFlowResult LeaveAndEnd() => new(ActionFlowKind.LeaveAndEnd);

        public static ActionFlowResult LeaveAndRestart() => new(ActionFlowKind.LeaveAndRestart);
    }

    protected ExecuteActionRuntimeCursor CurrentRuntimeCursor => runtimeCursor;

    protected void ResetRuntimeCursor() => runtimeCursor = ExecuteActionRuntimeCursor.Empty;

    protected void SetRuntimeCursor
    (
        int stepIndex,
        int actionIndex = -1
    ) =>
        runtimeCursor = new(stepIndex, actionIndex);

    protected Task<ActionFlowResult> ExecuteStepAsync
    (
        PresetStep        step,
        int               stepIndex,
        CancellationToken cancellationToken
    ) =>
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
        var actions = step.Actions;
        var startActionIndex = startCursor is { HasAction: true, StepIndex: var cursorStepIndex } && cursorStepIndex == stepIndex ?
                                   startCursor.ActionIndex :
                                   0;

        return await ExecuteActionListAsync(stepIndex, step, actions, cancellationToken, startActionIndex);
    }

    protected async Task<ActionFlowResult> ExecuteActionListAsync
    (
        int                     stepIndex,
        PresetStep              step,
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
            SetRuntimeCursor(stepIndex, actionIndex);
            var action = actions[actionIndex];
            var result = await ExecuteActionAsync(stepIndex, step, actionIndex, action, actions.Count, cancellationToken);

            switch (result.Kind)
            {
                case ActionFlowKind.Continue:
                    actionIndex++;
                    break;
                case ActionFlowKind.JumpToAction:
                    actionIndex = result.Index;
                    break;
                case ActionFlowKind.JumpToStep:
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
        int               actionIndex,
        ExecuteActionBase action,
        int               currentActionCount,
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
                    BuildActionMessage(stepIndex, step, actionIndex, "等待条件达成"),
                    cancellationToken
                );
                return await ExecuteActionCoreAsync(stepIndex, step, actionIndex, action, currentActionCount, cancellationToken);

            case ConditionExecuteType.Skip:
                if (!conditionCollection.Evaluate(CreateConditionContext()))
                    return ActionFlowResult.Continue();

                return await ExecuteActionCoreAsync(stepIndex, step, actionIndex, action, currentActionCount, cancellationToken);

            case ConditionExecuteType.Repeat:
                while (ShouldRepeat(conditionCollection, executedCount))
                {
                    var result = await ExecuteActionCoreAsync(stepIndex, step, actionIndex, action, currentActionCount, cancellationToken);
                    executedCount++;
                    if (result.Kind != ActionFlowKind.Continue)
                        return result;

                    if (ShouldRepeat(conditionCollection, executedCount) && conditionCollection.IntervalMs > 0)
                        await DelayAsync(conditionCollection.IntervalMs, BuildActionMessage(stepIndex, step, actionIndex, "等待重复间隔"), cancellationToken);
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
        int               actionIndex,
        ExecuteActionBase action,
        int               currentActionCount,
        CancellationToken cancellationToken
    )
    {
        var actionLabel = BuildActionMessage(stepIndex, step, actionIndex, action.Name);

        switch (action)
        {
            case WaitMillisecondsAction waitMilliseconds:
                if (waitMilliseconds.Milliseconds > 0)
                    await DelayAsync(waitMilliseconds.Milliseconds, actionLabel, cancellationToken);
                return ActionFlowResult.Continue();

            case JumpToStepAction jumpToStep:
                var targetStepIndex = jumpToStep.StepIndex < 0 ?
                                          stepIndex :
                                          jumpToStep.StepIndex;
                ValidateStepIndex(targetStepIndex);
                SetRunningMessage(actionLabel);
                return ActionFlowResult.JumpToStep(targetStepIndex);

            case JumpToActionAction jumpToAction:
                ValidateActionIndex(jumpToAction.ActionIndex, currentActionCount);
                SetRunningMessage(actionLabel);
                return ActionFlowResult.JumpToAction(jumpToAction.ActionIndex);

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
                {
                    ExecuteCommandManager.Instance().ExecuteCommandComplexLocation
                    (
                        gameCommandComplex.Command,
                        gameCommandComplex.Location,
                        gameCommandComplex.Param1,
                        gameCommandComplex.Param2,
                        gameCommandComplex.Param3,
                        gameCommandComplex.Param4
                    );
                }
                else
                {
                    ExecuteCommandManager.Instance().ExecuteCommandComplex
                    (
                        gameCommandComplex.Command,
                        gameCommandComplex.Target,
                        gameCommandComplex.Param1,
                        gameCommandComplex.Param2,
                        gameCommandComplex.Param3,
                        gameCommandComplex.Param4
                    );
                }

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
                                       actionIndex,
                                       action,
                                       currentActionCount,
                                       actionLabel,
                                       cancellationToken
                                   );
                if (customResult is { } result)
                    return result;

                throw new InvalidOperationException($"不支持的执行动作类型: {action.Kind}");
            }
        }
    }

    protected static string BuildActionMessage
    (
        int        stepIndex,
        PresetStep step,
        int        actionIndex,
        string     suffix
    ) =>
        $"步骤 {stepIndex}: {step.Name} / 动作 {actionIndex}: {suffix}";

    protected bool ShouldRepeat
    (
        ConditionCollection conditionCollection,
        int                 executedCount
    )
    {
        if (conditionCollection.MaxExecuteCount > 0 && executedCount >= conditionCollection.MaxExecuteCount)
            return false;

        if (executedCount < conditionCollection.MinExecuteCount)
            return true;

        if (conditionCollection is { IsEmpty: true, Negate: true })
            return false;

        return !conditionCollection.Evaluate(CreateConditionContext());
    }

    protected abstract ConditionContext CreateConditionContext();

    protected abstract void SetRunningMessage
    (
        string message
    );

    protected abstract void ValidateStepIndex
    (
        int stepIndex
    );

    protected abstract void ValidateActionIndex
    (
        int actionIndex,
        int currentActionCount
    );

    protected abstract void LeaveDuty();

    protected abstract Task LeaveDutyAndRestartAsync
    (
        string            message,
        CancellationToken cancellationToken
    );

    protected virtual Task<ActionFlowResult?> ExecuteCustomActionCoreAsync
    (
        int               stepIndex,
        PresetStep        step,
        int               actionIndex,
        ExecuteActionBase action,
        int               currentActionCount,
        string            actionLabel,
        CancellationToken cancellationToken
    ) =>
        Task.FromResult<ActionFlowResult?>(null);

    protected async Task RunCommandsAsync
    (
        string            commands,
        string            actionLabel,
        CancellationToken cancellationToken
    )
    {
        if (string.IsNullOrWhiteSpace(commands))
            return;

        foreach (var command in commands.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (command.StartsWith("/wait", StringComparison.OrdinalIgnoreCase))
            {
                var split = command.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

                if (split.Length == 2 && int.TryParse(split[1], out var waitTime))
                {
                    await DelayAsync(waitTime, $"{actionLabel} - 特殊文本等待", cancellationToken);
                    continue;
                }
            }

            SetRunningMessage($"{actionLabel} - {command}");
            ChatManager.Instance().SendCommand(command);
            await Task.Delay(100, cancellationToken);
        }
    }

    protected async Task ExecuteNearestInteractAsync
    (
        string            sourceName,
        CancellationToken cancellationToken
    )
    {
        var target = PresetTargetResolver.FindNearestInteractableObject();

        if (target == null)
        {
            SetRunningMessage($"未找到可交互物体: {sourceName}");
            return;
        }

        await WaitUntilAsync
        (
            () => !DService.Instance().Condition.IsOnMount         &&
                  !DService.Instance().Condition.IsOccupiedInEvent &&
                  UIModule.IsScreenReady()                         &&
                  target.TargetInteract(),
            $"交互最近可交互物体: {sourceName}",
            cancellationToken
        );

        PresetTargetResolver.OpenObjectInteraction(target);
    }

    protected async Task ExecuteMovementActionAsync
    (
        MoveToPositionAction action,
        string               actionLabel,
        CancellationToken    cancellationToken
    )
    {
        if (action.Position == default)
            return;

        switch (action.MoveType)
        {
            case MoveType.简单移动:
                SetRunningMessage(actionLabel);
                Movement.StartPathfindMovement(action.Position, cancellationToken);
                break;
            case MoveType.寻路:
                SetRunningMessage(actionLabel);
                Movement.StartVnavmeshMovement(action.Position, cancellationToken);
                break;
            case MoveType.无:
            case MoveType.传送:
            default:
                SetRunningMessage(actionLabel);
                MovementController.Teleport(action.Position);
                break;
        }

        await Task.CompletedTask;
    }

    protected async Task WaitUntilAsync
    (
        Func<bool>        predicate,
        string            message,
        CancellationToken cancellationToken,
        int               intervalMs = 100
    )
    {
        SetRunningMessage(message);
        while (!predicate())
            await Task.Delay(intervalMs, cancellationToken);
    }

    protected async Task DelayAsync
    (
        int               delayMs,
        string            message,
        CancellationToken cancellationToken
    )
    {
        SetRunningMessage(message);
        await Task.Delay(delayMs, cancellationToken);
    }
}
