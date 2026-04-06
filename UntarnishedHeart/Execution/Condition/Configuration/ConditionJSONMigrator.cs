using System.Collections.Frozen;
using Newtonsoft.Json.Linq;
using UntarnishedHeart.Execution.Condition.Configuration.Migrators;
using UntarnishedHeart.Internal.Configuration.Json;

namespace UntarnishedHeart.Execution.Condition.Configuration;

internal sealed class ConditionJSONMigrator : VersionedJsonMigratorBase<ConditionBase>
{
    internal const int CurrentJSONVersion = 4;

    private static readonly FrozenDictionary<int, JsonObjectMigratorBase> MigratorsInternal =
        new JsonObjectMigratorBase[]
        {
            new ConditionV1ToV2Migrator(),
            new ConditionV2ToV3Migrator(),
            new ConditionV3ToV4Migrator()
        }.ToFrozenDictionary(migrator => migrator.FromVersion);

    internal static ConditionJSONMigrator Instance { get; } = new();

    protected override string DisplayName => "条件";

    protected override int CurrentVersion => CurrentJSONVersion;

    protected override int LegacyVersion => 1;

    protected override IReadOnlyDictionary<int, JsonObjectMigratorBase> Migrators => MigratorsInternal;

    protected override int ResolveMissingVersion(JObject jsonObject) =>
        jsonObject["Kind"] is null
            ? LegacyVersion
            : 2;
}
