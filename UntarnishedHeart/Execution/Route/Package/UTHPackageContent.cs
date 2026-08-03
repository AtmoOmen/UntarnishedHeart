using PresetModel = UntarnishedHeart.Execution.Preset.Preset;

namespace UntarnishedHeart.Execution.Route.Package;

internal sealed class UTHPackageContent
(
    Route             route,
    List<PresetModel> presets
)
{
    public Route Route { get; } = route;

    public List<PresetModel> Presets { get; } = presets;
}
