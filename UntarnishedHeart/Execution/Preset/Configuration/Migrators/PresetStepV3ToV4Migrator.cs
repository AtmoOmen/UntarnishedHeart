using System.Numerics;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UntarnishedHeart.Execution.Condition.Configuration;
using UntarnishedHeart.Execution.Condition.Enums;
using UntarnishedHeart.Execution.ExecuteAction.Configuration;
using UntarnishedHeart.Execution.ExecuteAction.Enums;
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

        migratedMoveAction["Version"] = ExecuteActionJSONMigrator.CurrentJSONVersion;
        migratedMoveAction.Remove("WaitForArrival");

        yield return migratedMoveAction;

        if (!waitForArrival || position == default)
            yield break;

        yield return CreateArrivalWaitAction(position);
    }

    private static JObject CreateArrivalWaitAction(Vector3 position) =>
        new()
        {
            ["Version"]      = ExecuteActionJSONMigrator.CurrentJSONVersion,
            ["Kind"]         = ExecuteActionKind.Wait.ToString(),
            ["Milliseconds"] = 0,
            ["Condition"]    = CreatePositionRangeConditionCollection(position)
        };

    private static JObject CreatePositionRangeConditionCollection(Vector3 position) =>
        new()
        {
            ["Version"]         = ConditionCollectionJSONMigrator.CurrentJSONVersion,
            ["Conditions"]      = new JArray(CreatePositionRangeCondition(position)),
            ["RelationType"]    = ConditionRelationType.And.ToString(),
            ["ExecuteType"]     = ConditionExecuteType.Wait.ToString(),
            ["MinExecuteCount"] = 1,
            ["MaxExecuteCount"] = 1,
            ["IntervalMs"]      = 0
        };

    private static JObject CreatePositionRangeCondition(Vector3 position) =>
        new()
        {
            ["Version"]        = ConditionJSONMigrator.CurrentJSONVersion,
            ["Kind"]           = ConditionDetectType.PositionRange.ToString(),
            ["ComparisonType"] = PresenceComparisonType.Has.ToString(),
            ["Range"]          = CreatePositionRange(position)
        };

    private static JObject CreatePositionRange(Vector3 position)
    {
        var range = new PositionRange
        {
            Center = position,
            Radius = 4f
        };

        return JObject.FromObject(range);
    }
}
