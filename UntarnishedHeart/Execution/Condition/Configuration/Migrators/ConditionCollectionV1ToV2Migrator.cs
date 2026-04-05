using Newtonsoft.Json.Linq;
using UntarnishedHeart.Execution.Condition.Enums;
using UntarnishedHeart.Internal.Configuration.Json;

namespace UntarnishedHeart.Execution.Condition.Configuration.Migrators;

internal sealed class ConditionCollectionV1ToV2Migrator : JsonObjectMigratorBase
{
    public override int FromVersion => 1;

    public override int ToVersion => 2;

    public override JObject Migrate(JObject jsonObject)
    {
        var migrated = (JObject)jsonObject.DeepClone();

        var executeTypeText = migrated["ExecuteType"]?.Value<string>();
        if (string.Equals(executeTypeText, "Pass", StringComparison.OrdinalIgnoreCase))
            migrated["ExecuteType"] = ConditionExecuteType.Skip.ToString();

        migrated["MinExecuteCount"] ??= 1;
        migrated["MaxExecuteCount"] ??= executeTypeText is "Repeat" ? 0 : 1;

        if (migrated["IntervalMs"] is null && migrated["TimeValue"] is { } timeValue)
            migrated["IntervalMs"] = timeValue.Type == JTokenType.Float ? (int)timeValue.Value<float>() : timeValue.Value<int>();

        migrated.Remove("TimeValue");
        return migrated;
    }
}
