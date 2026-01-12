using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Client.UI;
using UntarnishedHeart.Managers;
using UntarnishedHeart.Windows;

namespace UntarnishedHeart.Executor;

/// <summary>
///     路线执行状态
/// </summary>
public enum RouteExecutorState
{
    /// <summary>
    ///     未开始
    /// </summary>
    NotStarted,

    /// <summary>
    ///     运行中
    /// </summary>
    Running,

    /// <summary>
    ///     等待执行器完成
    /// </summary>
    WaitingForExecutor,

    /// <summary>
    ///     已完成
    /// </summary>
    Completed,

    /// <summary>
    ///     已停止
    /// </summary>
    Stopped,

    /// <summary>
    ///     出错
    /// </summary>
    Error
}

/// <summary>
///     路线执行器
/// </summary>
public class RouteExecutor : IDisposable
{
    /// <summary>
    ///     取消令牌源
    /// </summary>
    private CancellationTokenSource? cancelToken;

    /// <summary>
    ///     主执行任务
    /// </summary>
    private Task? executionTask;

    /// <summary>
    ///     构造函数
    /// </summary>
    /// <param name="route">要执行的路线</param>
    public RouteExecutor(Route route) =>
        Steps = route.Steps;

    /// <summary>
    ///     路线步骤列表
    /// </summary>
    public List<RouteStep> Steps { get; set; }

    /// <summary>
    ///     当前步骤索引
    /// </summary>
    public int CurrentStepIndex { get; private set; }

    /// <summary>
    ///     当前执行的预设执行器
    /// </summary>
    public Executor? CurrentExecutor { get; private set; }

    /// <summary>
    ///     当前执行状态
    /// </summary>
    public RouteExecutorState State { get; private set; } = RouteExecutorState.NotStarted;

    /// <summary>
    ///     路线是否正在运行
    /// </summary>
    public bool IsRunning => State == RouteExecutorState.Running || State == RouteExecutorState.WaitingForExecutor;

    /// <summary>
    ///     路线是否已完成
    /// </summary>
    public bool IsFinished => State == RouteExecutorState.Completed;

    /// <summary>
    ///     是否已释放
    /// </summary>
    public bool IsDisposed { get; private set; }


    /// <summary>
    ///     当前运行消息
    /// </summary>
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
                var executorMessage = CurrentExecutor.RunningMessage;
                if (!string.IsNullOrEmpty(executorMessage))
                    stepInfo += $" - {executorMessage}";
            }

            return stepInfo;
        }
    }

    /// <summary>
    ///     本轮已完成副本次数
    /// </summary>
    private int CompletedDutyCount { get; set; }

    /// <summary>
    ///     释放资源
    /// </summary>
    public void Dispose()
    {
        if (IsDisposed) return;

        Stop();
        DisposeCurrentExecutor();

        cancelToken?.Dispose();
        cancelToken = null;

        // 等待执行任务完成（如果正在运行）
        try
        {
            executionTask?.Wait(TimeSpan.FromSeconds(5));
        }
        catch (AggregateException)
        {
            // 忽略取消异常
        }

        executionTask = null;

        IsDisposed = true;
    }

    /// <summary>
    ///     开始执行路线
    /// </summary>
    public async Task StartAsync()
    {
        if (IsRunning || Steps.Count == 0) return;

        State              = RouteExecutorState.Running;
        CurrentStepIndex   = 0;
        CompletedDutyCount = 0;

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
            Chat($"路线执行出错: {ex.Message}", Main.UTHPrefix);
        }
    }

    /// <summary>
    ///     开始执行路线（同步版本，用于向后兼容）
    /// </summary>
    public void Start() =>
        _ = Task.Run(async () => await StartAsync());

    /// <summary>
    ///     停止执行路线
    /// </summary>
    public void Stop()
    {
        if (State is RouteExecutorState.NotStarted or RouteExecutorState.Completed or RouteExecutorState.Stopped)
            return;

        cancelToken?.Cancel();
        State = RouteExecutorState.Stopped;

        DisposeCurrentExecutor();

    }

    /// <summary>
    ///     异步执行整个路线
    /// </summary>
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
            Chat("路线执行完成", Main.UTHPrefix);
        }
    }

    /// <summary>
    ///     异步执行当前步骤
    /// </summary>
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
                    Chat($"未知的步骤类型: {currentStep.StepType}", Main.UTHPrefix);
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
            Chat($"执行步骤时出错: {ex.Message}", Main.UTHPrefix);
        }
    }

    /// <summary>
    ///     异步执行切换预设步骤
    /// </summary>
    /// <param name="step">路线步骤</param>
    /// <param name="cancellationToken">取消令牌</param>
    private async Task ExecuteSwitchPresetStepAsync(RouteStep step, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(step.PresetName))
        {
            Chat("预设名称为空，跳过此步骤", Main.UTHPrefix);
            MoveToNextStep();
            return;
        }

        var currentPreset = CurrentExecutor?.ExecutorPreset?.Name ?? string.Empty;

        // 查找预设
        var preset = Service.Config.Presets.FirstOrDefault(p => p.Name == step.PresetName);

        if (preset is not { IsValid: true })
        {
            DisposeCurrentExecutor();

            Chat($"无法找到有效预设: {step.PresetName}", Main.UTHPrefix);
            MoveToNextStep();
            return;
        }

        if (step.Name != currentPreset)
        {
            Debug("预设发生变化, 重置副本计数");
            CompletedDutyCount = 0;
        }

        // 释放当前执行器
        DisposeCurrentExecutor();

        Chat($"开始执行预设: {preset.Name}", Main.UTHPrefix);

        // 创建执行器（构造函数会自动开始执行）
        CurrentExecutor = new Executor(preset, 1, step.DutyOptions);

        // 设置状态为等待执行器完成
        State = RouteExecutorState.WaitingForExecutor;

        // 等待执行器完成
        await WaitForExecutorCompletionAsync(cancellationToken);
        await Task.WaitForConditionAsync(() => UIModule.IsScreenReady() && GameState.ContentFinderCondition == 0);
        await Task.Delay(1000, cancellationToken);

        // 预设执行完成后，根据AfterPresetAction执行相应动作
        if (State != RouteExecutorState.Error && State != RouteExecutorState.Stopped)
        {
            State = RouteExecutorState.Running;
            var jumpIndex = step.AfterPresetAction == RouteStepActionType.JumpToStep ? step.AfterPresetJumpIndex : 0;
            ExecuteAction(step.AfterPresetAction, jumpIndex);
        }
    }

    /// <summary>
    ///     执行条件判断步骤
    /// </summary>
    /// <param name="step">路线步骤</param>
    private void ExecuteConditionCheckStep(RouteStep step)
    {
        Chat($"检查条件: {step.ConditionType.GetDescription()}", Main.UTHPrefix);

        var conditionMet = CheckCondition(step);
        var actionType   = conditionMet ? step.TrueAction : step.FalseAction;
        var jumpIndex    = conditionMet ? step.TrueJumpIndex : step.FalseJumpIndex;

        Chat($"条件{(conditionMet ? "满足" : "不满足")}，执行动作: {actionType.GetDescription()}", Main.UTHPrefix);

        ExecuteAction(actionType, jumpIndex);
    }

    /// <summary>
    ///     检查条件是否满足
    /// </summary>
    /// <param name="step">路线步骤</param>
    /// <returns>条件是否满足</returns>
    private bool CheckCondition(RouteStep step) => EvaluateCondition(step);

    /// <summary>
    ///     执行指定的动作
    /// </summary>
    /// <param name="actionType">动作类型</param>
    /// <param name="jumpIndex">跳转索引</param>
    private void ExecuteAction(RouteStepActionType actionType, int jumpIndex)
    {
        switch (actionType)
        {
            case RouteStepActionType.RepeatCurrentStep:
                // 重复当前步骤，不改变CurrentStepIndex
                Chat("重复当前步骤", Main.UTHPrefix);
                break;

            case RouteStepActionType.JumpToStep:
                // 跳转到指定步骤
                if (jumpIndex >= 0 && jumpIndex < Steps.Count)
                {
                    CurrentStepIndex = jumpIndex;
                    Chat($"跳转到步骤 {jumpIndex}: {Steps[jumpIndex].Name}", Main.UTHPrefix);
                }
                else
                    Chat($"无效的跳转索引: {jumpIndex}", Main.UTHPrefix);

                break;

            case RouteStepActionType.EndRoute:
                // 结束路线执行
                Chat("结束路线执行", Main.UTHPrefix);
                Stop();
                break;

            case RouteStepActionType.GoToPreviousStep:
                // 回到上一步
                if (CurrentStepIndex > 0)
                {
                    CurrentStepIndex--;
                    Chat($"回到上一步: {Steps[CurrentStepIndex].Name}", Main.UTHPrefix);
                }
                else
                    Chat("已经是第一步，无法回到上一步", Main.UTHPrefix);

                break;

            case RouteStepActionType.GoToNextStep:
                // 顺延下一步
                if (CurrentStepIndex < Steps.Count - 1)
                {
                    CurrentStepIndex++;
                    Chat($"顺延到下一步: {Steps[CurrentStepIndex].Name}", Main.UTHPrefix);
                }
                else
                {
                    Chat("已经是最后一步，路线执行完成", Main.UTHPrefix);
                    State = RouteExecutorState.Completed;
                }

                break;
        }
    }

    /// <summary>
    ///     评估条件
    /// </summary>
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

    /// <summary>
    ///     获取条件值
    /// </summary>
    private unsafe int GetConditionValue(ConditionType conditionType, int extraID) =>
        conditionType switch
        {
            ConditionType.PlayerLevel                => LocalPlayerState.CurrentLevel,
            ConditionType.OptimalPartyRecommendation => PlayerState.Instance()->PlayerCommendations,
            ConditionType.CompletedDutyCount         => CompletedDutyCount,
            ConditionType.AchievementCount => (int)(AchievementManager.Instance().TryGetAchievement((uint)extraID, out var achievementInfo)
                                                        ? achievementInfo.Current
                                                        : 0),
            ConditionType.ItemCount => (int)LocalPlayerState.GetItemCount((uint)extraID),
            _                       => 0
        };

    /// <summary>
    ///     等待执行器完成
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    private async Task WaitForExecutorCompletionAsync(CancellationToken cancellationToken)
    {
        while (State == RouteExecutorState.WaitingForExecutor && !cancellationToken.IsCancellationRequested)
        {
            // 检查执行器是否完成
            if (IsExecutorCompleted() && GameState.ContentFinderCondition == 0 && UIModule.IsScreenReady())
            {
                Chat("执行器已完成", Main.UTHPrefix);
                State = RouteExecutorState.Running;
                CompletedDutyCount++;
                break;
            }

            await Task.Delay(100, cancellationToken);
        }
    }

    /// <summary>
    ///     检查执行器是否完成
    /// </summary>
    /// <returns>执行器是否完成</returns>
    private bool IsExecutorCompleted()
    {
        if (CurrentExecutor == null) return true;

        // 检查执行器是否已完成
        return CurrentExecutor.IsFinished || CurrentExecutor.CurrentRound == 1;
    }

    /// <summary>
    ///     移动到下一步
    /// </summary>
    private void MoveToNextStep() =>
        CurrentStepIndex++;

    /// <summary>
    ///     释放当前执行器
    /// </summary>
    private void DisposeCurrentExecutor()
    {
        CurrentExecutor?.Dispose();
        CurrentExecutor = null;
    }
}
