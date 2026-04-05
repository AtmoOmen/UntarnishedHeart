using Newtonsoft.Json.Linq;
using UntarnishedHeart.Execution.CommandCondition.Legacy;
using UntarnishedHeart.Internal.Configuration.Json;

namespace UntarnishedHeart.Execution.CommandCondition.Configuration.Migrators;

internal sealed class CommandSingleConditionV1ToV2Migrator : JsonObjectMigratorBase
{
    public override int FromVersion => 1;

    public override int ToVersion => 2;

    public override JObject Migrate(JObject jsonObject)
    {
        var migrated = CommandSingleCondition.MigrateLegacyV1ToV2
        (
            CommandSingleConditionJsonConverter.ReadEnum(jsonObject["DetectType"],     Enums.CommandDetectType.Health),
            CommandSingleConditionJsonConverter.ReadEnum(jsonObject["ComparisonType"], CommandComparisonType.LessThan),
            CommandSingleConditionJsonConverter.ReadEnum(jsonObject["TargetType"],     Enums.CommandTargetType.Target),
            CommandSingleConditionJsonConverter.ReadFloat(jsonObject["Value"])
        );

        return CommandSingleConditionJsonConverter.SerializeToJObject(migrated);
    }
}
