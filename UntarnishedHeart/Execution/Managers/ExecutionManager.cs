using UntarnishedHeart.Execution.Preset;
using UntarnishedHeart.Execution.Route;

namespace UntarnishedHeart.Execution.Managers;

internal static class ExecutionManager
{
    public static PresetExecutor? PresetExecutor { get; private set; }
    public static RouteExecutor?  RouteExecutor  { get; private set; }

    public static void Dispose()
    {
        DisposePresetExecutor();
        StopRouteExecutor();
    }

    public static void DisposePresetExecutor()
    {
        PresetExecutor?.Dispose();
        PresetExecutor = null;
    }

    public static void StopRouteExecutor()
    {
        RouteExecutor?.Stop();
        RouteExecutor = null;
    }

    public static void StartSimpleExecution(Preset.Preset preset, PresetExecutorRunOptions runOptions)
    {
        DisposePresetExecutor();
        PresetExecutor = new(preset, runOptions);
        PresetExecutor.Start();
    }

    public static void StartRouteExecution(Route.Route route)
    {
        StopRouteExecutor();
        RouteExecutor = new(route);
        RouteExecutor.Start();
    }

    public static bool TryRequestNearestInteract()
    {
        if (PresetExecutor is { IsDisposed: false, Completion.IsCompleted: false } presetExecutor)
        {
            presetExecutor.RequestNearestInteract();
            return true;
        }

        if (RouteExecutor?.CurrentExecutor is { IsDisposed: false, Completion.IsCompleted: false } routeExecutor)
        {
            routeExecutor.RequestNearestInteract();
            return true;
        }

        return false;
    }

    public static void ManualEnqueueNewRound() => PresetExecutor?.ManualEnqueueNewRound();
}
