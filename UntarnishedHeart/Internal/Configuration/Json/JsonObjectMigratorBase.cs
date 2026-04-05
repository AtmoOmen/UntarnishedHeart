using Newtonsoft.Json.Linq;

namespace UntarnishedHeart.Internal.Configuration.Json;

internal abstract class JsonObjectMigratorBase
{
    public abstract int FromVersion { get; }

    public abstract int ToVersion { get; }

    public abstract JObject Migrate(JObject jsonObject);
}
