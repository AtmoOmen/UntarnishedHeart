using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UntarnishedHeart.Execution.Condition.Configuration.Migrators;
using UntarnishedHeart.Execution.Condition.Enums;

namespace UntarnishedHeart.Execution.Condition.Configuration;

public sealed class ConditionCollectionJSONConverter : JsonConverter<ConditionCollection>
{
    public override void WriteJson(JsonWriter writer, ConditionCollection? value, JsonSerializer serializer)
    {
        if (value is null)
        {
            writer.WriteNull();
            return;
        }

        SerializeToJObject(value, serializer).WriteTo(writer);
    }

    public override ConditionCollection? ReadJson
    (
        JsonReader           reader,
        Type                 objectType,
        ConditionCollection? existingValue,
        bool                 hasExistingValue,
        JsonSerializer       serializer
    )
    {
        if (reader.TokenType == JsonToken.Null)
            return null;

        var token = JToken.Load(reader);
        if (token.Type != JTokenType.Object)
            return null;

        var jsonObject = ConditionCollectionJSONMigrator.Instance.MigrateToLatest((JObject)token);
        return DeserializeCurrent(jsonObject, serializer);
    }

    internal static JObject SerializeToJObject(ConditionCollection value, JsonSerializer serializer) =>
        new()
        {
            ["Version"]         = ConditionCollectionJSONMigrator.CurrentJSONVersion,
            ["Conditions"]      = JToken.FromObject(value.Conditions, serializer),
            ["RelationType"]    = value.RelationType.ToString(),
            ["ExecuteType"]     = value.ExecuteType.ToString(),
            ["MinExecuteCount"] = value.MinExecuteCount,
            ["MaxExecuteCount"] = value.MaxExecuteCount,
            ["IntervalMs"]      = value.IntervalMs
        };

    internal static ConditionCollection DeserializeCurrent(JObject jsonObject, JsonSerializer serializer) =>
        new()
        {
            Conditions      = ConditionJSONConverter.ReadObject(jsonObject["Conditions"], serializer, new List<ConditionBase>()),
            RelationType    = ConditionJSONConverter.ReadEnum(jsonObject["RelationType"], ConditionRelationType.And),
            ExecuteType     = ConditionJSONConverter.ReadEnum(jsonObject["ExecuteType"],  ConditionExecuteType.Wait),
            MinExecuteCount = (int)ConditionJSONConverter.ReadUInt(jsonObject["MinExecuteCount"] ?? new JValue(1)),
            MaxExecuteCount = ConditionJSONConverter.ReadInt(jsonObject["MaxExecuteCount"], 1),
            IntervalMs      = ConditionJSONConverter.ReadInt(jsonObject["IntervalMs"])
        };
}
