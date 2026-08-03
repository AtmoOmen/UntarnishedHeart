using Newtonsoft.Json.Linq;
using UntarnishedHeart.Execution.ExecuteAction.Configuration;
using UntarnishedHeart.Internal.Configuration.Json;

namespace UntarnishedHeart.Execution.Preset.Configuration.Migrators;

internal sealed class PresetStepV5ToV6Migrator : JsonObjectMigratorBase
{
    public override int FromVersion => 5;

    public override int ToVersion => 6;

    public override JObject Migrate
    (
        JObject jsonObject
    )
    {
        var migrated = (JObject)jsonObject.DeepClone();
        var actions  = new JArray();

        if (migrated["Actions"] is JArray sourceActions)
        {
            for (var actionIndex = 0; actionIndex < sourceActions.Count; actionIndex++)
            {
                if (sourceActions[actionIndex] is not JObject actionObject)
                {
                    actions.Add(sourceActions[actionIndex].DeepClone());
                    continue;
                }

                actions.Add(MigrateAction(actionObject, actionIndex));
            }
        }

        migrated["Actions"] = actions;
        migrated["Version"] = 6;
        return migrated;
    }

    private static JObject MigrateAction
    (
        JObject actionObject,
        int     actionIndex
    )
    {
        var kind = ReadKind(actionObject);
        if (kind is null)
            return actionObject;

        var migrated = (JObject)actionObject.DeepClone();

        if (string.Equals(kind, "RestartCurrentStep", StringComparison.Ordinal))
        {
            migrated["TypeId"]    = "JumpToStep";
            migrated["StepIndex"] = -1;
        }
        else if (string.Equals(kind, "RestartCurrentAction", StringComparison.Ordinal))
        {
            migrated["TypeId"]      = "JumpToAction";
            migrated["ActionIndex"] = actionIndex;
        }
        else
            return actionObject;

        migrated.Remove("Kind");
        migrated["Version"] = ExecuteActionJSONMigrator.CurrentJSONVersion;
        return migrated;
    }

    private static string? ReadKind
    (
        JObject actionObject
    )
    {
        var typeID = actionObject["TypeId"]?.Value<string>();
        if (!string.IsNullOrWhiteSpace(typeID))
            return typeID;

        return actionObject["Kind"]?.Value<string>();
    }
}
