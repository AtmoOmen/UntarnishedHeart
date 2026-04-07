using Newtonsoft.Json.Linq;
using UntarnishedHeart.Internal.Configuration.Json;

namespace UntarnishedHeart.Execution.Condition.Configuration.Migrators;

internal sealed class ConditionV4ToV5Migrator : JsonObjectMigratorBase
{
    public override int FromVersion => 4;

    public override int ToVersion => 5;

    public override JObject Migrate(JObject jsonObject)
    {
        var migrated = (JObject)jsonObject.DeepClone();
        var kind     = ConditionJSONConverter.ReadConditionKind(migrated["Kind"]);
        migrated["TypeId"] = ConditionJsonTypeRegistry.Instance.GetTypeID(kind);
        migrated.Remove("Kind");
        return migrated;
    }
}
