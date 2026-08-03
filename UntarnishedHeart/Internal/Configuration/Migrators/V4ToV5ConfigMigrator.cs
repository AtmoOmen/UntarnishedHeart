using UntarnishedHeart.Execution.Preset;

namespace UntarnishedHeart.Internal.Configuration.Migrators;

internal sealed class V4ToV5ConfigMigrator : ConfigMigratorBase
{
    public override int FromVersion => 4;

    public override int ToVersion => 5;

    public override void Migrate
    (
        PluginConfig config
    )
    {
        foreach (var route in config.Routes)
            PresetReferenceBinder.BindUnboundReferences(route, config.Presets);
    }
}
