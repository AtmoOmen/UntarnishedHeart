using Newtonsoft.Json.Linq;
using UntarnishedHeart.Internal.Configuration.Json;

namespace UntarnishedHeart.Execution.Preset.Configuration.Migrators;

internal sealed class PresetStepV1ToV2Migrator : JsonObjectMigratorBase
{
    public override int FromVersion => 1;

    public override int ToVersion => 2;

    public override JObject Migrate(JObject jsonObject)
    {
        var migrated = (JObject)jsonObject.DeepClone();

        if (migrated["Condition"] is null && migrated["CommandCondition"] is { } legacyCondition)
            migrated["Condition"] = legacyCondition;

        migrated.Remove("CommandCondition");
        return migrated;
    }
}
