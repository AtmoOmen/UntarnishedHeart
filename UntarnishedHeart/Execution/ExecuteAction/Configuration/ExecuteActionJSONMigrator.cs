using System.Collections.Frozen;
using Newtonsoft.Json.Linq;
using UntarnishedHeart.Execution.ExecuteAction.Configuration.Migrators;
using UntarnishedHeart.Internal.Configuration.Json;

namespace UntarnishedHeart.Execution.ExecuteAction.Configuration;

internal sealed class ExecuteActionJSONMigrator : VersionedJsonMigratorBase<ExecuteActionBase>
{
    internal const int CurrentJSONVersion = 3;

    private static readonly FrozenDictionary<int, JsonObjectMigratorBase> MigratorsInternal =
        new JsonObjectMigratorBase[]
        {
            new ExecuteActionV1ToV2Migrator(),
            new ExecuteActionV2ToV3Migrator()
        }.ToFrozenDictionary(migrator => migrator.FromVersion);

    internal static ExecuteActionJSONMigrator Instance { get; } = new();

    protected override string DisplayName => "执行动作";

    protected override int CurrentVersion => CurrentJSONVersion;

    protected override int LegacyVersion => 1;

    protected override IReadOnlyDictionary<int, JsonObjectMigratorBase> Migrators => MigratorsInternal;

    protected override int ResolveMissingVersion(JObject jsonObject) =>
        jsonObject["TypeId"] is not null
            ? CurrentVersion
            : LegacyVersion;
}
