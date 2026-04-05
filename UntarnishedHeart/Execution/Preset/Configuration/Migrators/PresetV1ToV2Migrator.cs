using Newtonsoft.Json.Linq;
using UntarnishedHeart.Internal.Configuration.Json;

namespace UntarnishedHeart.Execution.Preset.Configuration.Migrators;

internal sealed class PresetV1ToV2Migrator : JsonObjectMigratorBase
{
    public override int FromVersion => 1;

    public override int ToVersion => 2;

    public override JObject Migrate(JObject jsonObject) => (JObject)jsonObject.DeepClone();
}
