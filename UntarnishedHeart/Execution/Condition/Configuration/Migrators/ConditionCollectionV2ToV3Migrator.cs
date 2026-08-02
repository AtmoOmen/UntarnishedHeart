using Newtonsoft.Json.Linq;
using UntarnishedHeart.Execution.Condition.Enums;
using UntarnishedHeart.Internal.Configuration.Json;

namespace UntarnishedHeart.Execution.Condition.Configuration.Migrators;

internal sealed class ConditionCollectionV2ToV3Migrator : JsonObjectMigratorBase
{
    public override int FromVersion => 2;

    public override int ToVersion => 3;

    public override JObject Migrate(JObject jsonObject)
    {
        var migrated = (JObject)jsonObject.DeepClone();
        var executeTypeText = migrated["ExecuteType"]?.Value<string>();

        if (string.Equals(executeTypeText, "Sustain", StringComparison.OrdinalIgnoreCase))
        {
            migrated["ExecuteType"] = nameof(ConditionExecuteType.Repeat);
            migrated["Negate"]      = true;
        }
        else
        {
            migrated["Negate"] = false;
        }

        migrated["Version"] = 3;
        return migrated;
    }
}
