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
            ConditionJsonConverter.ReadEnum(jsonObject["DetectType"],     ConditionDetectType.Health),
            ConditionJsonConverter.ReadEnum(jsonObject["ComparisonType"], ConditionComparisonType.LessThan),
            ConditionJsonConverter.ReadEnum(jsonObject["TargetType"],     ConditionTargetType.Target),
            ConditionJsonConverter.ReadFloat(jsonObject["Value"])
        );

        return ConditionJsonConverter.SerializeToJObject(migrated, JsonSerializer.CreateDefault());
    }
}
