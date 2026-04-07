using Newtonsoft.Json.Linq;
using UntarnishedHeart.Execution.ExecuteAction.Enums;
using UntarnishedHeart.Execution.Preset;
using UntarnishedHeart.Internal.Configuration.Json;

namespace UntarnishedHeart.Execution.ExecuteAction.Configuration.Migrators;

internal sealed class ExecuteActionV2ToV3Migrator : JsonObjectMigratorBase
{
    public override int FromVersion => 2;

    public override int ToVersion => 3;

    public override JObject Migrate(JObject jsonObject)
    {
        var migrated = (JObject)jsonObject.DeepClone();
        var kind     = PresetStepJsonConverter.ReadEnum(migrated["Kind"], ExecuteActionKind.Wait);
        migrated["TypeId"] = ExecuteActionJsonTypeRegistry.Instance.GetTypeID(kind);
        migrated.Remove("Kind");
        return migrated;
    }
}
