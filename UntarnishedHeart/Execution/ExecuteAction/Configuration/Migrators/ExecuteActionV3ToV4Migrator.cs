using Newtonsoft.Json.Linq;
using UntarnishedHeart.Internal.Configuration.Json;

namespace UntarnishedHeart.Execution.ExecuteAction.Configuration.Migrators;

internal sealed class ExecuteActionV3ToV4Migrator : JsonObjectMigratorBase
{
    public override int FromVersion => 3;

    public override int ToVersion => 4;

    public override JObject Migrate(JObject jsonObject)
    {
        var migrated = (JObject)jsonObject.DeepClone();
        var typeID   = migrated["TypeId"]?.Value<string>();

        if (typeID is "RestartCurrentStep" or "RestartCurrentAction")
            throw new InvalidOperationException($"执行动作 {typeID} 需要在步骤迁移中处理");

        migrated["Version"] = 4;
        return migrated;
    }
}
