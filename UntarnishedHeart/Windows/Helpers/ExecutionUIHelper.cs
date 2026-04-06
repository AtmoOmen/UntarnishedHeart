using Dalamud.Game.ClientState.Conditions;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using OmenTools.OmenService;
using UntarnishedHeart.Execution.Enums;
using UntarnishedHeart.Execution.Managers;
using UntarnishedHeart.Execution.Preset;
using UntarnishedHeart.Execution.Route;
using UntarnishedHeart.Internal;

namespace UntarnishedHeart.Windows.Components;

internal static class ExecutionUIHelper
{
    public static ExecutionStatusViewState CreateStatusViewState()
    {
        if (PluginConfig.Instance().CurrentExecutionMode == ExecutionMode.Preset)
        {
            var presetExecutor                     = ExecutionManager.PresetExecutor;
            var isRunning                          = presetExecutor is { IsDisposed: false, Completion.IsCompleted: false };
            var maxRoundText                       = presetExecutor?.MaxRound == -1 ? "无限" : $"{presetExecutor?.MaxRound ?? 0}";
            var progressText                       = $"{presetExecutor?.CurrentRound ?? 0} / {maxRoundText}";
            var isStopAfterDutyCompletionRequested = presetExecutor?.IsStopAfterDutyCompletionRequested == true;
            var message                            = BuildRunningMessage(presetExecutor?.RunningMessage ?? string.Empty, isStopAfterDutyCompletionRequested);

            return new
            (
                "预设模式",
                isRunning,
                "轮次",
                progressText,
                message,
                "结束",
                StopSimpleExecution,
                isRunning,
                isStopAfterDutyCompletionRequested ? "取消" : "完成副本后结束",
                isStopAfterDutyCompletionRequested ? CancelSimpleStopAfterDutyCompletionRequest : RequestSimpleStopAfterDutyCompletion,
                isRunning,
                isStopAfterDutyCompletionRequested
            );
        }

        var routeExecutor                           = ExecutionManager.RouteExecutor;
        var isRouteRunning                          = routeExecutor?.IsRunning == true;
        var totalSteps                              = routeExecutor?.Steps.Count ?? 0;
        var currentStepNumber                       = routeExecutor is { CurrentStepIndex: >= 0 } runningRoute ? runningRoute.CurrentStepIndex + 1 : 0;
        var routeProgress                           = $"{currentStepNumber} / {totalSteps}";
        var isRouteStopAfterDutyCompletionRequested = routeExecutor?.IsStopAfterDutyCompletionRequested == true;
        var routeMessage                            = BuildRunningMessage(routeExecutor?.RunningMessage ?? string.Empty, isRouteStopAfterDutyCompletionRequested);

        return new
        (
            "路线模式",
            isRouteRunning,
            "步骤",
            routeProgress,
            routeMessage,
            "停止",
            StopRouteExecution,
            isRouteRunning,
            isRouteStopAfterDutyCompletionRequested ? "取消" : "完成副本后结束",
            isRouteStopAfterDutyCompletionRequested ? CancelRouteStopAfterDutyCompletionRequest : RequestRouteStopAfterDutyCompletion,
            isRouteRunning,
            isRouteStopAfterDutyCompletionRequested
        );
    }

    public static bool CanStartCurrentMode() => GetStartDisabledReasons().Count == 0;

    public static IReadOnlyList<string> GetStartDisabledReasons()
    {
        var reasons = new List<string>();

        if (DService.Instance().Condition.IsBetweenAreas)
            reasons.Add("切换区域中");

        if (!GameState.IsLoggedIn)
            reasons.Add("尚未登录");

        if (PluginConfig.Instance().CurrentExecutionMode == ExecutionMode.Preset)
        {
            if (ExecutionManager.PresetExecutor is { IsDisposed: false, Completion.IsCompleted: false })
                reasons.Add("执行中");

            if (PluginConfig.Instance().Presets.Count == 0)
                reasons.Add("无预设");
            else if (GetSelectedPreset() is not { IsValid: true })
                reasons.Add("预设无效");

            return reasons;
        }

        if (ExecutionManager.RouteExecutor is { IsRunning: true })
            reasons.Add("执行中");

        if (PluginConfig.Instance().Routes.Count == 0)
            reasons.Add("无路线");
        else if (GetSelectedRoute() is not { IsValid: true })
            reasons.Add("路线未完成");

        return reasons;
    }

    public static void OpenStatusWindow() =>
        WindowManager.Instance().Get<ExecutionStatusWindow>().IsOpen = true;

    public static void StopCurrentExecution()
    {
        if (ExecutionManager.PresetExecutor is { IsDisposed: false, Completion.IsCompleted: false })
        {
            StopSimpleExecution();
            return;
        }

        if (ExecutionManager.RouteExecutor is { IsRunning: true })
            StopRouteExecution();
    }

    public static void StopSimpleExecution()
    {
        ExecutionManager.DisposePresetExecutor();
        CancelDutyQueueIfNeeded();
    }

    public static void StopRouteExecution()
    {
        ExecutionManager.StopRouteExecutor();
        CancelDutyQueueIfNeeded();
    }

    public static void RequestSimpleStopAfterDutyCompletion() =>
        ExecutionManager.PresetExecutor?.RequestStopAfterDutyCompletion();

    public static void CancelSimpleStopAfterDutyCompletionRequest() =>
        ExecutionManager.PresetExecutor?.CancelStopAfterDutyCompletionRequest();

    public static void RequestRouteStopAfterDutyCompletion() =>
        ExecutionManager.RouteExecutor?.RequestStopAfterDutyCompletion();

    public static void CancelRouteStopAfterDutyCompletionRequest() =>
        ExecutionManager.RouteExecutor?.CancelStopAfterDutyCompletionRequest();

    private static Preset? GetSelectedPreset()
    {
        var selectedPresetIndex = CollectionToolbar.NormalizeSelectedIndex(MainWindow.SelectedPresetIndexAccessor, PluginConfig.Instance().Presets.Count);
        return selectedPresetIndex >= 0 ? PluginConfig.Instance().Presets[selectedPresetIndex] : null;
    }

    private static Route? GetSelectedRoute()
    {
        var selectedRouteIndex = CollectionToolbar.NormalizeSelectedIndex(MainWindow.SelectedRouteIndexAccessor, PluginConfig.Instance().Routes.Count);
        return selectedRouteIndex >= 0 ? PluginConfig.Instance().Routes[selectedRouteIndex] : null;
    }

    private static string BuildRunningMessage(string runningMessage, bool isStopAfterDutyCompletionRequested)
    {
        if (!isStopAfterDutyCompletionRequested)
            return runningMessage;

        const string REQUEST_MESSAGE = "已请求完成副本后结束，等待当前或下一次副本完成并退出";
        return string.IsNullOrWhiteSpace(runningMessage) ? REQUEST_MESSAGE : $"{runningMessage}\n{REQUEST_MESSAGE}";
    }

    private static unsafe void CancelDutyQueueIfNeeded()
    {
        if (!DService.Instance().Condition[ConditionFlag.InDutyQueue])
            return;

        AgentId.ContentsFinder.SendEvent(0, 12, 0);
    }
}

internal readonly record struct ExecutionStatusViewState
(
    string ModeName,
    bool   IsRunning,
    string ProgressLabel,
    string ProgressText,
    string RunningMessage,
    string StopLabel,
    Action StopAction,
    bool   CanStop,
    string DeferredStopLabel,
    Action DeferredStopAction,
    bool   CanDeferredStop,
    bool   IsStopAfterDutyCompletionRequested
);
