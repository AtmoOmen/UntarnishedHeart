using UntarnishedHeart.Internal;

namespace UntarnishedHeart.Internal.Configuration.Migrators;

internal sealed class V1ToV2ConfigMigrator : ConfigMigratorBase
{
    public override int FromVersion => 1;

    public override int ToVersion => 2;

    public override void Migrate(PluginConfig config) { }
}
