using Newtonsoft.Json.Linq;
using UntarnishedHeart.Internal.Configuration.Json;

namespace UntarnishedHeart.Execution.Condition.Configuration.Migrators;

internal sealed class ConditionV3ToV4Migrator : JsonObjectMigratorBase
{
    public override int FromVersion => 3;

    public override int ToVersion => 4;

    public override JObject Migrate(JObject jsonObject) => (JObject)jsonObject.DeepClone();
}
