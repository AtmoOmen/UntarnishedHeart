using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Client.UI;
using OmenTools.Dalamud;
using OmenTools.OmenService;
using UntarnishedHeart.Execution.Enums;
using UntarnishedHeart.Execution.Preset;
using UntarnishedHeart.Execution.Route.Enums;
using UntarnishedHeart.Internal;

namespace UntarnishedHeart.Execution.Route;

/// <summary>
///     路线执行器
/// </summary>
public class RouteExecutor
(
    Route route
) : IDisposable
{
    private CancellationTokenSource? cancelToken;
    private Task?                    executionTask;

    public List<RouteStep> Steps { get; set; } = route.Steps;

    public int CurrentStepIndex { get; private set; }

    public PresetExecutor? CurrentExecutor { get; private set; }

    public RouteExecutorState State { get; private set; } = RouteExecutorState.NotStarted;

    public bool IsRunning => State is RouteExecutorState.Running or RouteExecutorState.WaitingForExecutor;

    public bool IsFinished => State == RouteExecutorState.Completed;

    public bool IsDisposed { get; private set; }

    public bool IsStopAfterDutyCompletionRequested { get; private set; }

    public string RunningMessage
    {
        get
        {
            if (!IsRunning) return "路线未运行";
            if (IsFinished) return "路线已完成";
            if (CurrentStepIndex >= Steps.Count) return "路线索引超出范围";

            var currentStep = Steps[CurrentStepIndex];
            var stepInfo    = $"步骤 {CurrentStepIndex}: {currentStep.Name}";

            if (CurrentExecutor != null)
            {
                var executorMessage = CurrentExecutor.Progress.RunningMessage;
                if (!string.IsNullOrEmpty(executorMessage))
                    stepInfo += $" - {executorMessage}";
            }

            return stepInfo;
        }
    }

    private int CompletedDutyCount { get; set; }

    public void Dispose()
    {
        if (IsDisposed) return;

        Stop();

        try
        {
            executionTask?.Wait(TimeSpan.FromSeconds(5));
        }
        catch (AggregateException)
        {
            // 忽略取消异常
        }

        cancelToken?.Dispose();
        cancelToken   = null;
        executionTask = null;

        DisposeCurrentExecutor();

        IsDisposed = true;
    }

    public async Task StartAsync()
    {
        if (IsRunning || Steps.Count == 0) return;

        State              = RouteExecutorState.Running;
        CurrentStepIndex   = 0;
        CompletedDutyCount = 0;
        IsStopAfterDutyCompletionRequested = false;

        cancelToken?.Dispose();
        cancelToken = new CancellationTokenSource();

        try
        {
            executionTask = ExecuteRouteAsync(cancelToken.Token);
            await executionTask;
        }
        catch (OperationCanceledException)
        {
            State = RouteExecutorState.Stopped;
        }
        catch (Exception ex)
        {
            State = RouteExecutorState.Error;
            NotifyHelper.Instance().Chat($"路线执行出错: {ex.Message}");
        }
    }

    public void Start() =>
        _ = DService.Instance().Framework.Run(StartAsync);

    public void Stop()
    {
        if (State is RouteExecutorState.NotStarted or RouteExecutorState.Completed or RouteExecutorState.Stopped)
            return;

        IsStopAfterDutyCompletionRequested = false;
        cancelToken?.Cancel();
        State = RouteExecutorState.Stopped;

        DisposeCurrentExecutor();
    }

    public bool RequestStopAfterDutyCompletion()
    {
        if (!IsRunning)
            return false;

        IsStopAfterDutyCompletionRequested = true;
        CurrentExecutor?.RequestStopAfterDutyCompletion();
        return true;
    }

    public bool CancelStopAfterDutyCompletionRequest()
    {
        if (!IsStopAfterDutyCompletionRequested)
            return false;

        IsStopAfterDutyCompletionRequested = false;
        CurrentExecutor?.CancelStopAfterDutyCompletionRequest();
        return true;
    }

    private async Task ExecuteRouteAsync(CancellationToken cancellationToken)
    {
        while (CurrentStepIndex < Steps.Count && !cancellationToken.IsCancellationRequested)
        {
            await ExecuteCurrentStepAsync(cancellationToken);

            if (State == RouteExecutorState.Error)
                break;
        }

        if (CurrentStepIndex >= Steps.Count && State == RouteExecutorState.Running)
        {
            State = RouteExecutorState.Completed;
            NotifyHelper.Instance().Chat("路线执行完成");
        }
    }

    private async Task ExecuteCurrentStepAsync(CancellationToken cancellationToken)
    {
        if (CurrentStepIndex >= Steps.Count)
            return;

        var currentStep = Steps[CurrentStepIndex];

        try
        {
            switch (currentStep.StepType)
            {
                case RouteStepType.SwitchPreset:
                    await ExecuteSwitchPresetStepAsync(currentStep, cancellationToken);
                    break;
                case RouteStepType.ConditionCheck:
                    ExecuteConditionCheckStep(currentStep);
                    break;
                default:
                    NotifyHelper.Instance().Chat($"未知的步骤类型: {currentStep.StepType}");
                    MoveToNextStep();
                    break;
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            State = RouteExecutorState.Error;
            NotifyHelper.Instance().Chat($"执行步骤时出错: {ex.Message}");
        }
    }

    private async Task ExecuteSwitchPresetStepAsync(RouteStep step, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(step.PresetName))
        {
            NotifyHelper.Instance().Chat("预设名称为空，跳过此步骤");
            MoveToNextStep();
            return;
        }

        var currentPresetName = CurrentExecutor?.ExecutorPreset?.Name ?? string.Empty;
        var preset            = PluginConfig.Instance().Presets.FirstOrDefault(p => p.Name == step.PresetName);

        if (preset is not { IsValid: true })
        {
            DisposeCurrentExecutor();
            NotifyHelper.Instance().Chat($"无法找到有效预设: {step.PresetName}");
            MoveToNextStep();
            return;
        }

        if (!string.Equals(step.PresetName, currentPresetName, StringComparison.Ordinal))
        {
            DLog.Debug("预设发生变化, 重置副本计数");
            CompletedDutyCount = 0;
        }

        DisposeCurrentExecutor();

        NotifyHelper.Instance().Chat($"开始执行预设: {preset.Name}");

        CurrentExecutor = new PresetExecutor(preset, step.DutyOptions.ToRunOptions());
        if (IsStopAfterDutyCompletionRequested)
            CurrentExecutor.RequestStopAfterDutyCompletion();

        CurrentExecutor.Start();

        State = RouteExecutorState.WaitingForExecutor;

        var result = await CurrentExecutor.Completion.WaitAsync(cancellationToken);

        switch (result.EndReason)
        {
            case ExecutorEndReason.Error:
                State = RouteExecutorState.Error;
                NotifyHelper.Instance().Chat($"预设执行出错: {result.ErrorMessage}");
                return;
            case ExecutorEndReason.Stopped:
                State = RouteExecutorState.Stopped;
                return;
            case ExecutorEndReason.CompletedAfterDuty:
                State = RouteExecutorState.Stopped;
                NotifyHelper.Instance().Chat("已在副本完成并退出后停止路线执行");
                return;
            case ExecutorEndReason.InvalidPreset:
                NotifyHelper.Instance().Chat($"预设无效，跳过此步骤: {step.PresetName}");
                State = RouteExecutorState.Running;
                MoveToNextStep();
                return;
        }

        var condition = DService.Instance().Condition;
        while ((condition.IsBoundByDuty  ||
                condition.IsBetweenAreas ||
                !UIModule.IsScreenReady()) &&
               !cancellationToken.IsCancellationRequested)
            await Task.Delay(100, cancellationToken);

        CompletedDutyCount += (int)result.CompletedRounds;
        State              =  RouteExecutorState.Running;

        var jumpIndex = step.AfterPresetAction == RouteStepActionType.JumpToStep ? step.AfterPresetJumpIndex : 0;
        ExecuteAction(step.AfterPresetAction, jumpIndex);
    }

    private void ExecuteConditionCheckStep(RouteStep step)
    {
        NotifyHelper.Instance().Chat($"检查条件: {step.ConditionType.GetDescription()}");

        var conditionMet = CheckCondition(step);
        var actionType   = conditionMet ? step.TrueAction : step.FalseAction;
        var jumpIndex    = conditionMet ? step.TrueJumpIndex : step.FalseJumpIndex;

        NotifyHelper.Instance().Chat($"条件{(conditionMet ? "满足" : "不满足")}，执行动作: {actionType.GetDescription()}");

        ExecuteAction(actionType, jumpIndex);
    }

    private bool CheckCondition(RouteStep step) => EvaluateCondition(step);

    private void ExecuteAction(RouteStepActionType actionType, int jumpIndex)
    {
        switch (actionType)
        {
            case RouteStepActionType.RepeatCurrentStep:
                NotifyHelper.Instance().Chat("重复当前步骤");
                break;

            case RouteStepActionType.JumpToStep:
                if (jumpIndex >= 0 && jumpIndex < Steps.Count)
                {
                    CurrentStepIndex = jumpIndex;
                    NotifyHelper.Instance().Chat($"跳转到步骤 {jumpIndex}: {Steps[jumpIndex].Name}");
                }
                else
                    NotifyHelper.Instance().Chat($"无效的跳转索引: {jumpIndex}");

                break;

            case RouteStepActionType.EndRoute:
                NotifyHelper.Instance().Chat("结束路线执行");
                Stop();
                break;

            case RouteStepActionType.GoToPreviousStep:
                if (CurrentStepIndex > 0)
                {
                    CurrentStepIndex--;
                    NotifyHelper.Instance().Chat($"回到上一步: {Steps[CurrentStepIndex].Name}");
                }
                else
                    NotifyHelper.Instance().Chat("已经是第一步，无法回到上一步");

                break;

            case RouteStepActionType.GoToNextStep:
                if (CurrentStepIndex < Steps.Count - 1)
                {
                    CurrentStepIndex++;
                    NotifyHelper.Instance().Chat($"顺延到下一步: {Steps[CurrentStepIndex].Name}");
                }
                else
                {
                    NotifyHelper.Instance().Chat("已经是最后一步，路线执行完成");
                    State = RouteExecutorState.Completed;
                }

                break;
        }
    }

    private bool EvaluateCondition(RouteStep step)
    {
        var actualValue   = GetConditionValue(step.ConditionType, step.ExtraID);
        var expectedValue = step.ConditionValue;

        return step.ComparisonType switch
        {
            ComparisonType.GreaterThan        => actualValue > expectedValue,
            ComparisonType.LessThan           => actualValue < expectedValue,
            ComparisonType.Equal              => actualValue == expectedValue,
            ComparisonType.GreaterThanOrEqual => actualValue >= expectedValue,
            ComparisonType.LessThanOrEqual    => actualValue <= expectedValue,
            ComparisonType.NotEqual           => actualValue != expectedValue,
            _                                 => false
        };
    }

    private unsafe int GetConditionValue(RouteConditionType conditionType, int extraID) =>
        conditionType switch
        {
            RouteConditionType.PlayerLevel                => LocalPlayerState.CurrentLevel,
            RouteConditionType.OptimalPartyRecommendation => PlayerState.Instance()->PlayerCommendations,
            RouteConditionType.CompletedDutyCount         => CompletedDutyCount,
            RouteConditionType.AchievementCount => (int)(AchievementManager.Instance().TryGetAchievement((uint)extraID, out var achievementInfo)
                                                             ? achievementInfo.Current
                                                             : 0),
            RouteConditionType.ItemCount => (int)LocalPlayerState.GetItemCount((uint)extraID),
            _                            => 0
        };

    private void MoveToNextStep() =>
        CurrentStepIndex++;

    private void DisposeCurrentExecutor()
    {
        CurrentExecutor?.Dispose();
        CurrentExecutor = null;
    }
}
