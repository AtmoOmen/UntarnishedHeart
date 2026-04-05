using System.Collections.Frozen;
using Newtonsoft.Json.Linq;
using UntarnishedHeart.Execution.Condition.Configuration.Migrators;
using UntarnishedHeart.Internal.Configuration.Json;

namespace UntarnishedHeart.Execution.Condition.Configuration;

internal sealed class ConditionCollectionJSONMigrator : VersionedJsonMigratorBase<ConditionCollection>
{
    internal const int CurrentJSONVersion = 2;

    private static readonly FrozenDictionary<int, JsonObjectMigratorBase> MigratorsInternal =
        new JsonObjectMigratorBase[]
        {
            new ConditionCollectionV1ToV2Migrator()
        }.ToFrozenDictionary(migrator => migrator.FromVersion);

    internal static ConditionCollectionJSONMigrator Instance { get; } = new();

    protected override string DisplayName => "条件组";

    protected override int CurrentVersion => CurrentJSONVersion;

    protected override int LegacyVersion => 1;

    protected override IReadOnlyDictionary<int, JsonObjectMigratorBase> Migrators => MigratorsInternal;

    protected override int ResolveMissingVersion(JObject jsonObject) => LegacyVersion;
}
