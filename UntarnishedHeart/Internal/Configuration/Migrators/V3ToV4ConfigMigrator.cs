using UntarnishedHeart.Windows.Components;

namespace UntarnishedHeart.Internal.Configuration.Migrators;

internal sealed class V3ToV4ConfigMigrator : ConfigMigratorBase
{
    public override int FromVersion => 3;

    public override int ToVersion => 4;

    public override void Migrate(PluginConfig config)
    {
        config.SelectedPresetIndex  = config.Presets.Count > 0 ? 0 : -1;
        config.SelectedRouteIndex   = CollectionToolbar.NormalizeSelectedIndex(config.SelectedRouteIndex, config.Routes.Count);
        config.UnlockMainWindowSize = false;
    }
}
