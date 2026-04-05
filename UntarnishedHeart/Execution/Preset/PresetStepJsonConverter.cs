using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UntarnishedHeart.Execution.Preset.Configuration;

namespace UntarnishedHeart.Execution.Preset;

public sealed class PresetStepJsonConverter : JsonConverter<PresetStep>
{
    public override void WriteJson(JsonWriter writer, PresetStep? value, JsonSerializer serializer)
    {
        if (value is null)
        {
            writer.WriteNull();
            return;
        }

        SerializeToJObject(value, serializer).WriteTo(writer);
    }

    public override PresetStep? ReadJson
    (
        JsonReader     reader,
        Type           objectType,
        PresetStep?    existingValue,
        bool           hasExistingValue,
        JsonSerializer serializer
    )
    {
        if (reader.TokenType == JsonToken.Null)
            return null;

        var token = JToken.Load(reader);
        if (token.Type != JTokenType.Object)
            return null;

        var jsonObject = PresetStepJSONMigrator.Instance.MigrateToLatest((JObject)token);
        return DeserializeCurrent(jsonObject, serializer);
    }

    internal static JObject SerializeToJObject(PresetStep value, JsonSerializer serializer) =>
        new()
        {
            ["Version"]      = PresetStepJSONMigrator.CurrentJSONVersion,
            ["Name"]         = value.Name,
            ["Remark"]       = value.Remark,
            ["EnterActions"] = JToken.FromObject(value.EnterActions, serializer),
            ["BodyActions"]  = JToken.FromObject(value.BodyActions,  serializer),
            ["ExitActions"]  = JToken.FromObject(value.ExitActions,  serializer)
        };

    internal static PresetStep DeserializeCurrent(JObject jsonObject, JsonSerializer serializer) =>
        new()
        {
            Name         = ReadString(jsonObject["Name"]),
            Remark       = ReadString(jsonObject["Remark"]),
            EnterActions = ReadObject(jsonObject["EnterActions"], serializer, new List<ExecuteAction.ExecuteAction>()),
            BodyActions  = ReadObject(jsonObject["BodyActions"],  serializer, new List<ExecuteAction.ExecuteAction>()),
            ExitActions  = ReadObject(jsonObject["ExitActions"],  serializer, new List<ExecuteAction.ExecuteAction>())
        };

    internal static bool ReadBool(JToken? token, bool fallback = false) =>
        token is null
            ? fallback
            : token.Type switch
            {
                JTokenType.Boolean                                                         => token.Value<bool>(),
                JTokenType.Integer                                                         => token.Value<int>() != 0,
                JTokenType.String when bool.TryParse(token.Value<string>(), out var value) => value,
                _                                                                          => fallback
            };

    internal static int ReadInt(JToken? token, int fallback = 0) =>
        token is null
            ? fallback
            : token.Type switch
            {
                JTokenType.Integer                                                        => token.Value<int>(),
                JTokenType.Float                                                          => (int)token.Value<float>(),
                JTokenType.String when int.TryParse(token.Value<string>(), out var value) => value,
                _                                                                         => fallback
            };

    internal static uint ReadUInt(JToken? token, uint fallback = 0) => (uint)Math.Max(0, ReadInt(token, (int)fallback));

    internal static string ReadString(JToken? token, string fallback = "") =>
        token?.Type == JTokenType.Null
            ? fallback
            : token?.Value<string>() ?? fallback;

    internal static T ReadObject<T>(JToken? token, JsonSerializer serializer, T fallback)
    {
        if (token is null)
            return fallback;

        return token.ToObject<T>(serializer) ?? fallback;
    }

    internal static TEnum ReadEnum<TEnum>(JToken? token, TEnum fallback)
        where TEnum : struct, Enum
    {
        if (token is null)
            return fallback;

        if (token.Type == JTokenType.Integer)
        {
            try
            {
                var raw            = token.Value<long>();
                var enumType       = typeof(TEnum);
                var underlyingType = Enum.GetUnderlyingType(enumType);
                var converted      = Convert.ChangeType(raw, underlyingType);

                if (Enum.IsDefined(enumType, converted))
                    return (TEnum)Enum.ToObject(enumType, converted);
            }
            catch (Exception)
            {
                return fallback;
            }
        }

        if (token.Type == JTokenType.String && Enum.TryParse<TEnum>(token.Value<string>(), true, out var value))
            return value;

        return fallback;
    }
}
