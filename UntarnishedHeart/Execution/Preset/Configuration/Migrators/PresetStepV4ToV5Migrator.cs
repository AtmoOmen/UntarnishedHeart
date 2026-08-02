using Newtonsoft.Json.Linq;
using UntarnishedHeart.Internal.Configuration.Json;

namespace UntarnishedHeart.Execution.Preset.Configuration.Migrators;

internal sealed class PresetStepV4ToV5Migrator : JsonObjectMigratorBase
{
    public override int FromVersion => 4;

    public override int ToVersion => 5;

    public override JObject Migrate(JObject jsonObject)
    {
        var migrated = (JObject)jsonObject.DeepClone();
        var actions  = new JArray();

        foreach (var phaseKey in new[] { "EnterActions", "BodyActions", "ExitActions" })
        {
            if (migrated[phaseKey] is not JArray phaseActions)
                continue;

            var phaseOffset = actions.Count;

            foreach (var actionToken in phaseActions)
            {
                if (actionToken is not JObject actionObject)
                {
                    actions.Add(actionToken.DeepClone());
                    continue;
                }

                var migratedAction = (JObject)actionObject.DeepClone();
                if (IsJumpToAction(migratedAction))
                {
                    var phaseIndex = PresetStepJsonConverter.ReadInt(migratedAction["ActionIndex"], -1);
                    migratedAction["ActionIndex"] = phaseIndex < 0 ? -1 : phaseOffset + phaseIndex;
                }

                actions.Add(migratedAction);
            }
        }

        migrated.Remove("EnterActions");
        migrated.Remove("BodyActions");
        migrated.Remove("ExitActions");
        migrated["Actions"] = actions;
        migrated["Version"] = 5;
        return migrated;
    }

    private static bool IsJumpToAction(JObject actionObject)
    {
        var typeID = PresetStepJsonConverter.ReadString(actionObject["TypeId"]);
        if (string.Equals(typeID, "JumpToAction", StringComparison.Ordinal))
            return true;

        return string.Equals(PresetStepJsonConverter.ReadString(actionObject["Kind"]), "JumpToAction", StringComparison.Ordinal);
    }
}
