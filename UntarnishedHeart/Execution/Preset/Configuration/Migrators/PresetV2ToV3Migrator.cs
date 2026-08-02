using Newtonsoft.Json.Linq;
using UntarnishedHeart.Internal.Configuration.Json;

namespace UntarnishedHeart.Execution.Preset.Configuration.Migrators;

internal sealed class PresetV2ToV3Migrator : JsonObjectMigratorBase
{
    public override int FromVersion => 2;

    public override int ToVersion => 3;

    public override JObject Migrate(JObject jsonObject)
    {
        var migrated = (JObject)jsonObject.DeepClone();
        var id       = migrated["ID"]?.Value<string>();

        if (string.IsNullOrWhiteSpace(id))
            migrated["ID"] = Guid.NewGuid().ToString("D");

        return migrated;
    }
}
