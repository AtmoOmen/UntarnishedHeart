using System.Numerics;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UntarnishedHeart.Execution.Condition;
using UntarnishedHeart.Execution.Condition.Enums;
using UntarnishedHeart.Execution.ExecuteAction.Configuration;
using UntarnishedHeart.Execution.ExecuteAction.Enums;
using UntarnishedHeart.Execution.ExecuteAction.Implementations;
using UntarnishedHeart.Execution.Models;
using UntarnishedHeart.Internal.Configuration.Json;

namespace UntarnishedHeart.Execution.Preset.Configuration.Migrators;

internal sealed class PresetStepV3ToV4Migrator : JsonObjectMigratorBase
{
    public override int FromVersion => 3;

    public override int ToVersion => 4;

    public override JObject Migrate(JObject jsonObject)
    {
        var migrated = (JObject)jsonObject.DeepClone();
        migrated["EnterActions"] = MigrateActionArray(migrated["EnterActions"]);
        migrated["BodyActions"]  = MigrateActionArray(migrated["BodyActions"]);
        migrated["ExitActions"]  = MigrateActionArray(migrated["ExitActions"]);
        return migrated;
    }

    private static JToken MigrateActionArray(JToken? token)
    {
        if (token is not JArray actions)
            return token?.DeepClone() ?? new JArray();

        var migrated = new JArray();

        foreach (var actionToken in actions)
        {
            if (actionToken is not JObject actionObject)
            {
                migrated.Add(actionToken.DeepClone());
                continue;
            }

            foreach (var newAction in ExpandAction(actionObject))
                migrated.Add(newAction);
        }

        return migrated;
    }

    private static IEnumerable<JObject> ExpandAction(JObject actionObject)
    {
        if (PresetStepJsonConverter.ReadEnum(actionObject["Kind"], ExecuteActionKind.Wait) != ExecuteActionKind.MoveToPosition)
        {
            yield return (JObject)actionObject.DeepClone();
            yield break;
        }

        var migratedMoveAction = (JObject)actionObject.DeepClone();
        var waitForArrival     = PresetStepJsonConverter.ReadBool(migratedMoveAction["WaitForArrival"]);
        var position           = PresetStepJsonConverter.ReadObject(migratedMoveAction["Position"], JsonSerializer.CreateDefault(), default(Vector3));

        migratedMoveAction["TypeId"] = ExecuteActionJsonTypeRegistry.Instance.GetTypeID(ExecuteActionKind.MoveToPosition);
        migratedMoveAction.Remove("Kind");
        migratedMoveAction["Version"] = ExecuteActionJSONMigrator.CurrentJSONVersion;
        migratedMoveAction.Remove("WaitForArrival");

        yield return migratedMoveAction;

        if (!waitForArrival || position == default)
            yield break;

        yield return CreateArrivalWaitAction(position);
    }

    private static JObject CreateArrivalWaitAction(Vector3 position) =>
        ExecuteActionJSONConverter.SerializeToJObject
        (
            new WaitMillisecondsAction
            {
                Milliseconds = 0,
                Condition    = CreatePositionRangeConditionCollection(position)
            },
            JsonSerializer.CreateDefault()
        );

    private static ConditionCollection CreatePositionRangeConditionCollection(Vector3 position) =>
        new()
        {
            Conditions = [CreatePositionRangeCondition(position)]
        };

    private static PositionRangeCondition CreatePositionRangeCondition(Vector3 position) =>
        new()
        {
            ComparisonType = PresenceComparisonType.Has,
            Range          = CreatePositionRange(position)
        };

    private static PositionRange CreatePositionRange(Vector3 position) =>
        new()
        {
            Center = position,
            Radius = 4f
        };
}
