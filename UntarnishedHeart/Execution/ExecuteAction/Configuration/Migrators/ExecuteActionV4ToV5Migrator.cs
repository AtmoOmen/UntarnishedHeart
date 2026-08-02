using Newtonsoft.Json.Linq;
using UntarnishedHeart.Internal.Configuration.Json;

namespace UntarnishedHeart.Execution.ExecuteAction.Configuration.Migrators;

internal sealed class ExecuteActionV4ToV5Migrator : JsonObjectMigratorBase
{
    public override int FromVersion => 4;

    public override int ToVersion => 5;

    public override JObject Migrate(JObject jsonObject)
    {
        var migrated = (JObject)jsonObject.DeepClone();

        if (migrated["TypeId"]?.Value<string>() != "ExecutePreset")
            return migrated;

        if (migrated["PresetID"] is null or { Type: JTokenType.Null })
            migrated["PresetID"] = string.Empty;

        return migrated;
    }
}
