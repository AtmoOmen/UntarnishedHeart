namespace UntarnishedHeart.Internal.Configuration.Migrators;

internal sealed class V2ToV3ConfigMigrator : ConfigMigratorBase
{
    public override int FromVersion => 2;

    public override int ToVersion => 3;

    public override void Migrate(PluginConfig config)
    {
    }
}
