using Newtonsoft.Json;
using UntarnishedHeart.Execution.Route.Package.Configuration;

namespace UntarnishedHeart.Execution.Route.Package;

[JsonConverter(typeof(UTHPackageJsonConverter))]
public sealed class UTHPackage
{
    public int Version { get; set; } = UTHPackageJSONMigrator.CurrentJSONVersion;

    public string RouteFile { get; set; } = "route.json";

    public List<string> PresetFiles { get; set; } = [];
}
