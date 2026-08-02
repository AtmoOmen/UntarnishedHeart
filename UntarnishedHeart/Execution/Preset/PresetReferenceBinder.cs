using UntarnishedHeart.Execution.ExecuteAction.Implementations;
using RouteModel = UntarnishedHeart.Execution.Route.Route;

namespace UntarnishedHeart.Execution.Preset;

internal static class PresetReferenceBinder
{
    public static int BindUnboundReferences(RouteModel route, IReadOnlyList<Preset> presets)
    {
        var unbound = 0;

        foreach (var step in route.Steps)
        {
            foreach (var action in step.Actions)
            {
                if (action is not ExecutePresetAction presetAction)
                    continue;

                if (!string.IsNullOrWhiteSpace(presetAction.PresetID))
                    continue;

                Preset? match = null;
                foreach (var candidate in presets)
                {
                    if (!string.Equals(candidate.Name, presetAction.PresetName, StringComparison.Ordinal))
                        continue;

                    if (match != null)
                    {
                        match = null;
                        break;
                    }

                    match = candidate;
                }

                if (match == null)
                {
                    unbound++;
                    continue;
                }

                presetAction.PresetID   = match.ID;
                presetAction.PresetName = match.Name;
            }
        }

        return unbound;
    }
}
