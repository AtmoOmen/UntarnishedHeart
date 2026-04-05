using System.Collections.Frozen;
using Newtonsoft.Json.Linq;
using UntarnishedHeart.Execution.Preset.Configuration.Migrators;
using UntarnishedHeart.Internal.Configuration.Json;

namespace UntarnishedHeart.Execution.Preset.Configuration;

internal sealed class PresetJSONMigrator : VersionedJsonMigratorBase<Preset>
{
    internal const int CurrentJSONVersion = 2;

    private static readonly FrozenDictionary<int, JsonObjectMigratorBase> MigratorsInternal =
        new JsonObjectMigratorBase[]
        {
            new PresetV1ToV2Migrator()
        }.ToFrozenDictionary(migrator => migrator.FromVersion);

    internal static PresetJSONMigrator Instance { get; } = new();

    protected override string DisplayName => "预设";

    protected override int CurrentVersion => CurrentJSONVersion;

    protected override int LegacyVersion => 1;

    protected override IReadOnlyDictionary<int, JsonObjectMigratorBase> Migrators => MigratorsInternal;

    protected override int ResolveMissingVersion(JObject jsonObject) => LegacyVersion;
}
