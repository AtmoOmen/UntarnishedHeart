using UntarnishedHeart.Execution.Common;
using UntarnishedHeart.Execution.Enums;
using UntarnishedHeart.Execution.Preset;
using UntarnishedHeart.Execution.Preset.Enums;
using UntarnishedHeart.Internal;
using PresetModel = UntarnishedHeart.Execution.Preset.Preset;
using RouteModel = UntarnishedHeart.Execution.Route.Route;

namespace UntarnishedHeart.Execution.Managers;

internal static class PendingExecutionStartManager
{
    private static PendingSelection<PresetModel>? PendingPresetSelection;
    private static PendingSelection<RouteModel>?  PendingRouteSelection;

    public static void SelectPreset(PresetModel preset, int presetIndex, ExecuteActionRuntimeCursor startCursor)
    {
        PersistSelection(ExecutionMode.Preset, presetIndex, null);
        PendingPresetSelection = new(preset, CloneCursor(startCursor));
    }

    public static void SelectRoute(RouteModel route, int routeIndex, ExecuteActionRuntimeCursor startCursor)
    {
        PersistSelection(ExecutionMode.Route, null, routeIndex);
        PendingRouteSelection = new(route, CloneCursor(startCursor));
    }

    public static ExecuteActionRuntimeCursor? GetPresetStartCursor(PresetModel? preset)
    {
        if (preset == null || PendingPresetSelection is not { } selection)
            return null;

        if (!ReferenceEquals(selection.Target, preset) || !IsValidCursor(preset.Steps, selection.StartCursor))
        {
            if (ReferenceEquals(selection.Target, preset))
                PendingPresetSelection = null;

            return null;
        }

        return CloneCursor(selection.StartCursor);
    }

    public static ExecuteActionRuntimeCursor? GetRouteStartCursor(RouteModel? route)
    {
        if (route == null || PendingRouteSelection is not { } selection)
            return null;

        if (!ReferenceEquals(selection.Target, route) || !IsValidCursor(route.Steps, selection.StartCursor))
        {
            if (ReferenceEquals(selection.Target, route))
                PendingRouteSelection = null;

            return null;
        }

        return CloneCursor(selection.StartCursor);
    }

    public static string? GetPresetDescription(PresetModel? preset)
    {
        var startCursor = GetPresetStartCursor(preset);
        return startCursor == null ? null : BuildDescription(startCursor);
    }

    public static string? GetRouteDescription(RouteModel? route)
    {
        var startCursor = GetRouteStartCursor(route);
        return startCursor == null ? null : BuildDescription(startCursor);
    }

    public static void ClearPreset(PresetModel? preset = null)
    {
        if (preset == null || PendingPresetSelection == null || ReferenceEquals(PendingPresetSelection.Target, preset))
            PendingPresetSelection = null;
    }

    public static void ClearRoute(RouteModel? route = null)
    {
        if (route == null || PendingRouteSelection == null || ReferenceEquals(PendingRouteSelection.Target, route))
            PendingRouteSelection = null;
    }

    private static void PersistSelection(ExecutionMode mode, int? presetIndex, int? routeIndex)
    {
        var config  = PluginConfig.Instance();
        var changed = false;

        if (config.CurrentExecutionMode != mode)
        {
            config.CurrentExecutionMode = mode;
            changed                     = true;
        }

        if (presetIndex is { } nextPresetIndex)
        {
            var normalizedPresetIndex = NormalizeIndex(nextPresetIndex, config.Presets.Count);

            if (config.SelectedPresetIndex != normalizedPresetIndex)
            {
                config.SelectedPresetIndex = normalizedPresetIndex;
                changed                    = true;
            }
        }

        if (routeIndex is { } nextRouteIndex)
        {
            var normalizedRouteIndex = NormalizeIndex(nextRouteIndex, config.Routes.Count);

            if (config.SelectedRouteIndex != normalizedRouteIndex)
            {
                config.SelectedRouteIndex = normalizedRouteIndex;
                changed                   = true;
            }
        }

        if (changed)
            config.Save();
    }

    private static bool IsValidCursor(List<PresetStep> steps, ExecuteActionRuntimeCursor cursor)
    {
        if (!cursor.HasStep || cursor.StepIndex < 0 || cursor.StepIndex >= steps.Count)
            return false;

        if (!cursor.HasPhase)
            return true;

        if (!Enum.IsDefined(cursor.Phase!.Value))
            return false;

        if (!cursor.HasAction)
            return true;

        var actions = cursor.Phase.Value switch
        {
            PresetStepPhase.Enter => steps[cursor.StepIndex].EnterActions,
            PresetStepPhase.Body  => steps[cursor.StepIndex].BodyActions,
            PresetStepPhase.Exit  => steps[cursor.StepIndex].ExitActions,
            _                     => null
        };

        return actions is not null && cursor.ActionIndex >= 0 && cursor.ActionIndex < actions.Count;
    }

    private static string BuildDescription(ExecuteActionRuntimeCursor startCursor)
    {
        var stepText = $"第 {startCursor.StepIndex} 步";
        if (!startCursor.HasPhase)
            return $"(将从{stepText}开始执行)";

        var phaseText = $"第 {GetPhaseOrder(startCursor.Phase!.Value)} 阶段";
        if (!startCursor.HasAction)
            return $"(将从{stepText}{phaseText}开始执行)";

        var actionText = $"第 {startCursor.ActionIndex} 动作";
        return $"(将从{stepText}{phaseText}{actionText}开始执行)";
    }

    private static int NormalizeIndex(int index, int count) => index >= 0 && index < count ? index : -1;

    private static int GetPhaseOrder(PresetStepPhase phase) =>
        phase switch
        {
            PresetStepPhase.Enter => 1,
            PresetStepPhase.Body  => 2,
            PresetStepPhase.Exit  => 3,
            _                     => 0
        };

    private static ExecuteActionRuntimeCursor CloneCursor(ExecuteActionRuntimeCursor cursor) =>
        new(cursor.StepIndex, cursor.Phase, cursor.ActionIndex);

    private sealed record PendingSelection<TTarget>
    (
        TTarget                    Target,
        ExecuteActionRuntimeCursor StartCursor
    )
        where TTarget : class;
}
