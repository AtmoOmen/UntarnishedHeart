namespace UntarnishedHeart.Internal.Configuration.Migrators;

internal abstract class ConfigMigratorBase
{
    public abstract int FromVersion { get; }

    public abstract int ToVersion { get; }

    public abstract void Migrate(PluginConfig config);
}
