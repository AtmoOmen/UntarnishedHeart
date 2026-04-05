using System.Collections.Frozen;
using UntarnishedHeart.Internal.Configuration.Migrators;

namespace UntarnishedHeart.Internal.Configuration;

internal static class PluginConfigMigrator
{
    internal const int LatestVersion = 2;

    private static readonly FrozenDictionary<int, ConfigMigratorBase> Migrators =
        new ConfigMigratorBase[]
        {
            new V0ToV1ConfigMigrator(),
            new V1ToV2ConfigMigrator()
        }.ToFrozenDictionary(migrator => migrator.FromVersion);

    internal static void Migrate(PluginConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);

        var didMigrate = false;

        if (config.Version < 0)
        {
            config.Version = 0;
            didMigrate     = true;
        }

        if (config.Version >= LatestVersion)
        {
            if (didMigrate)
                config.Save();

            return;
        }

        while (config.Version < LatestVersion)
        {
            if (!Migrators.TryGetValue(config.Version, out var migrator))
                throw new InvalidOperationException($"不支持的配置版本: {config.Version}");

            migrator.Migrate(config);
            config.Version = migrator.ToVersion;
            didMigrate     = true;
        }

        if (didMigrate)
            config.Save();
    }
}
