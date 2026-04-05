using Newtonsoft.Json.Linq;
using UntarnishedHeart.Internal.Configuration.Json;

namespace UntarnishedHeart.Execution.Condition.Configuration.Migrators;

internal sealed class ConditionV2ToV3Migrator : JsonObjectMigratorBase
{
    public override int FromVersion => 2;

    public override int ToVersion => 3;

    public override JObject Migrate(JObject jsonObject) => (JObject)jsonObject.DeepClone();
}
