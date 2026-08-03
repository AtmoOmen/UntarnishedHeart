using System.Collections.Frozen;
using UntarnishedHeart.Internal.Configuration.Json;

namespace UntarnishedHeart.Execution.Route.Package.Configuration;

internal sealed class UTHPackageJSONMigrator : VersionedJsonMigratorBase<UTHPackage>
{
    internal const int CurrentJSONVersion = 1;

    private static readonly FrozenDictionary<int, JsonObjectMigratorBase> MigratorsInternal =
        FrozenDictionary<int, JsonObjectMigratorBase>.Empty;

    internal static UTHPackageJSONMigrator Instance { get; } = new();

    protected override string DisplayName => "路线包清单";

    protected override int CurrentVersion => CurrentJSONVersion;

    protected override int LegacyVersion => 1;

    protected override IReadOnlyDictionary<int, JsonObjectMigratorBase> Migrators => MigratorsInternal;
}
