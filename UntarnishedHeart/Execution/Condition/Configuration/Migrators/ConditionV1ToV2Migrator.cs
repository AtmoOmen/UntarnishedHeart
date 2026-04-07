using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UntarnishedHeart.Execution.Condition.Enums;
using UntarnishedHeart.Execution.Condition.Legacy;
using UntarnishedHeart.Internal.Configuration.Json;

namespace UntarnishedHeart.Execution.Condition.Configuration.Migrators;

internal sealed class ConditionV1ToV2Migrator : JsonObjectMigratorBase
{
    public override int FromVersion => 1;

    public override int ToVersion => 2;

    public override JObject Migrate(JObject jsonObject)
    {
        var migrated = ConditionBase.MigrateLegacyV1ToV2
        (
            ConditionJSONConverter.ReadEnum(jsonObject["DetectType"],     ConditionDetectType.Health),
            ConditionJSONConverter.ReadEnum(jsonObject["ComparisonType"], ConditionComparisonType.LessThan),
            ConditionJSONConverter.ReadEnum(jsonObject["TargetType"],     ConditionTargetType.Target),
            ConditionJSONConverter.ReadFloat(jsonObject["Value"])
        );

        return ConditionJSONConverter.SerializeLegacyV2ToJObject(migrated, JsonSerializer.CreateDefault());
    }
}
