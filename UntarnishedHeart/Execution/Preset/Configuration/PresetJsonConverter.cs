using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace UntarnishedHeart.Execution.Preset.Configuration;

public sealed class PresetJsonConverter : JsonConverter<Preset>
{
    public override void WriteJson(JsonWriter writer, Preset? value, JsonSerializer serializer)
    {
        if (value is null)
        {
            writer.WriteNull();
            return;
        }

        SerializeToJObject(value, serializer).WriteTo(writer);
    }

    public override Preset? ReadJson
    (
        JsonReader     reader,
        Type           objectType,
        Preset?        existingValue,
        bool           hasExistingValue,
        JsonSerializer serializer
    )
    {
        if (reader.TokenType == JsonToken.Null)
            return null;

        var token = JToken.Load(reader);
        if (token.Type != JTokenType.Object)
            return null;

        var jsonObject = PresetJSONMigrator.Instance.MigrateToLatest((JObject)token);
        return DeserializeCurrent(jsonObject, serializer);
    }

    internal static JObject SerializeToJObject(Preset value, JsonSerializer serializer) =>
        new()
        {
            ["Version"]           = PresetJSONMigrator.CurrentJSONVersion,
            ["Name"]              = value.Name,
            ["Remark"]            = value.Remark,
            ["Zone"]              = value.Zone,
            ["Steps"]             = JToken.FromObject(value.Steps, serializer),
            ["AutoOpenTreasures"] = value.AutoOpenTreasures,
            ["DutyDelay"]         = value.DutyDelay
        };

    internal static Preset DeserializeCurrent(JObject jsonObject, JsonSerializer serializer) =>
        new()
        {
            Version           = PresetJSONMigrator.CurrentJSONVersion,
            Name              = PresetStepJsonConverter.ReadString(jsonObject["Name"]),
            Remark            = PresetStepJsonConverter.ReadString(jsonObject["Remark"]),
            Zone              = (ushort)Math.Max(0, PresetStepJsonConverter.ReadInt(jsonObject["Zone"])),
            Steps             = PresetStepJsonConverter.ReadObject(jsonObject["Steps"], serializer, new List<PresetStep>()),
            AutoOpenTreasures = PresetStepJsonConverter.ReadBool(jsonObject["AutoOpenTreasures"]),
            DutyDelay         = PresetStepJsonConverter.ReadInt(jsonObject["DutyDelay"], 500)
        };
}
