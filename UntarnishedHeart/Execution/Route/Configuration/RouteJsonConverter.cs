using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UntarnishedHeart.Execution.Preset;

namespace UntarnishedHeart.Execution.Route.Configuration;

public sealed class RouteJsonConverter : JsonConverter<Route>
{
    public override void WriteJson(JsonWriter writer, Route? value, JsonSerializer serializer)
    {
        if (value is null)
        {
            writer.WriteNull();
            return;
        }

        SerializeToJObject(value, serializer).WriteTo(writer);
    }

    public override Route? ReadJson
    (
        JsonReader     reader,
        Type           objectType,
        Route?         existingValue,
        bool           hasExistingValue,
        JsonSerializer serializer
    )
    {
        if (reader.TokenType == JsonToken.Null)
            return null;

        var token = JToken.Load(reader);
        if (token.Type != JTokenType.Object)
            return null;

        var jsonObject = RouteJSONMigrator.Instance.MigrateToLatest((JObject)token);
        return DeserializeCurrent(jsonObject, serializer);
    }

    internal static JObject SerializeToJObject(Route value, JsonSerializer serializer) =>
        new()
        {
            ["Version"] = RouteJSONMigrator.CurrentJSONVersion,
            ["Name"]    = value.Name,
            ["Remark"]  = value.Remark,
            ["Steps"]   = JToken.FromObject(value.Steps, serializer)
        };

    internal static Route DeserializeCurrent(JObject jsonObject, JsonSerializer serializer) =>
        new()
        {
            Version = RouteJSONMigrator.CurrentJSONVersion,
            Name    = PresetStepJsonConverter.ReadString(jsonObject["Name"]),
            Remark  = PresetStepJsonConverter.ReadString(jsonObject["Remark"]),
            Steps   = PresetStepJsonConverter.ReadObject(jsonObject["Steps"], serializer, new List<PresetStep>())
        };
}
