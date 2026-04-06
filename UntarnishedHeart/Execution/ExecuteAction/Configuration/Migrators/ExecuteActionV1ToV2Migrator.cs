using Newtonsoft.Json.Linq;
using UntarnishedHeart.Internal.Configuration.Json;

namespace UntarnishedHeart.Execution.ExecuteAction.Configuration.Migrators;

internal sealed class ExecuteActionV1ToV2Migrator : JsonObjectMigratorBase
{
    public override int FromVersion => 1;

    public override int ToVersion => 2;

    public override JObject Migrate(JObject jsonObject)
    {
        var migrated = (JObject)jsonObject.DeepClone();
        migrated.Remove("WaitForArrival");
        return migrated;
    }
}
