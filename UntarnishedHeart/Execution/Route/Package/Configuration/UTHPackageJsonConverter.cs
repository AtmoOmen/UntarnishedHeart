using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UntarnishedHeart.Execution.Preset;

namespace UntarnishedHeart.Execution.Route.Package.Configuration;

public sealed class UTHPackageJsonConverter : JsonConverter<UTHPackage>
{
    public override void WriteJson(JsonWriter writer, UTHPackage? value, JsonSerializer serializer)
    {
        if (value is null)
        {
            writer.WriteNull();
            return;
        }

        SerializeToJObject(value, serializer).WriteTo(writer);
    }

    public override UTHPackage? ReadJson
    (
        JsonReader     reader,
        Type           objectType,
        UTHPackage?    existingValue,
        bool           hasExistingValue,
        JsonSerializer serializer
    )
    {
        if (reader.TokenType == JsonToken.Null)
            return null;

        var token = JToken.Load(reader);
        if (token.Type != JTokenType.Object)
            return null;

        var jsonObject = UTHPackageJSONMigrator.Instance.MigrateToLatest((JObject)token);
        return DeserializeCurrent(jsonObject, serializer);
    }

    internal static JObject SerializeToJObject(UTHPackage value, JsonSerializer serializer) =>
        new()
        {
            ["Version"]     = UTHPackageJSONMigrator.CurrentJSONVersion,
            ["RouteFile"]   = value.RouteFile,
            ["PresetFiles"] = JToken.FromObject(value.PresetFiles, serializer)
        };

    internal static UTHPackage DeserializeCurrent(JObject jsonObject, JsonSerializer serializer) =>
        new()
        {
            Version     = UTHPackageJSONMigrator.CurrentJSONVersion,
            RouteFile   = PresetStepJsonConverter.ReadString(jsonObject["RouteFile"], "route.json"),
            PresetFiles = PresetStepJsonConverter.ReadObject(jsonObject["PresetFiles"], serializer, new List<string>())
        };
}
