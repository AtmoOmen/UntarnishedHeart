using System.Collections.Frozen;
using Newtonsoft.Json.Linq;
using UntarnishedHeart.Execution.Route.Configuration.Migrators;
using UntarnishedHeart.Internal.Configuration.Json;

namespace UntarnishedHeart.Execution.Route.Configuration;

internal sealed class RouteJSONMigrator : VersionedJsonMigratorBase<Route>
{
    internal const int CurrentJSONVersion = 2;

    private static readonly FrozenDictionary<int, JsonObjectMigratorBase> MigratorsInternal =
        new JsonObjectMigratorBase[]
        {
            new RouteV1ToV2Migrator()
        }.ToFrozenDictionary(migrator => migrator.FromVersion);

    internal static RouteJSONMigrator Instance { get; } = new();

    protected override int CurrentVersion => CurrentJSONVersion;

    protected override int LegacyVersion => 1;

    protected override IReadOnlyDictionary<int, JsonObjectMigratorBase> Migrators => MigratorsInternal;

    protected override int ResolveMissingVersion(JObject jsonObject) => LegacyVersion;
}
