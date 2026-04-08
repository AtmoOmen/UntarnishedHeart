using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UntarnishedHeart.Execution.Condition.Enums;
using UntarnishedHeart.Execution.Models;
using UntarnishedHeart.Internal.Configuration.Json;

namespace UntarnishedHeart.Execution.Condition.Configuration;

public sealed class ConditionJSONConverter : JsonConverter<ConditionBase>
{
    private const string TypeIDPropertyName = "TypeId";

    public override void WriteJson(JsonWriter writer, ConditionBase? value, JsonSerializer serializer)
    {
        if (value is null)
        {
            writer.WriteNull();
            return;
        }

        SerializeToJObject(value, serializer).WriteTo(writer);
    }

    public override ConditionBase? ReadJson
    (
        JsonReader     reader,
        Type           objectType,
        ConditionBase? existingValue,
        bool           hasExistingValue,
        JsonSerializer serializer
    )
    {
        if (reader.TokenType == JsonToken.Null)
            return null;

        var token = JToken.Load(reader);
        if (token.Type != JTokenType.Object)
            return null;

        var jsonObject = ConditionJSONMigrator.Instance.MigrateToLatest((JObject)token);
        return DeserializeCurrent(jsonObject);
    }

    internal static JObject SerializeToJObject(ConditionBase value, JsonSerializer serializer)
    {
        ArgumentNullException.ThrowIfNull(value);

        var concreteSerializer = PolymorphicJsonSerializerFactory.CreateConcreteTypeSerializer<ConditionBase>();
        var obj                = JObject.FromObject(value, concreteSerializer);
        obj["Version"]          = ConditionJSONMigrator.CurrentJSONVersion;
        obj[TypeIDPropertyName] = ConditionJsonTypeRegistry.Instance.GetTypeID(value);
        return obj;
    }

    internal static JObject SerializeLegacyV2ToJObject(ConditionBase value, JsonSerializer serializer)
    {
        var obj = new JObject
        {
            ["Kind"]   = value.Kind.ToString(),
            ["Name"]   = value.Name,
            ["Remark"] = value.Remark
        };

        switch (value)
        {
            case HealthCondition health:
                obj["ComparisonType"] = health.ComparisonType.ToString();
                obj["TargetType"]     = health.TargetType.ToString();
                obj["Threshold"]      = health.Threshold;
                break;

            case StatusCondition status:
                obj["ComparisonType"] = status.ComparisonType.ToString();
                obj["TargetType"]     = status.TargetType.ToString();
                obj["StatusId"]       = status.StatusID;
                break;

            case ActionCastCondition cast:
                obj["ComparisonType"] = cast.ComparisonType.ToString();
                obj["TargetType"]     = cast.TargetType.ToString();
                obj["Action"]         = JToken.FromObject(cast.Action, serializer);
                break;

            case ActionCooldownCondition cooldown:
                obj["ComparisonType"] = cooldown.ComparisonType.ToString();
                obj["Action"]         = JToken.FromObject(cooldown.Action, serializer);
                break;
        }

        return obj;
    }

    internal static ConditionBase DeserializeCurrent(JObject obj)
    {
        var typeID = ReadString(obj[TypeIDPropertyName]);
        if (string.IsNullOrWhiteSpace(typeID))
            throw new InvalidOperationException("条件缺少 TypeId");

        var runtimeType        = ConditionJsonTypeRegistry.Instance.GetRuntimeType(typeID);
        var concreteSerializer = PolymorphicJsonSerializerFactory.CreateConcreteTypeSerializer<ConditionBase>();

        var condition = obj.ToObject(runtimeType, concreteSerializer) as ConditionBase ??
                        throw new InvalidOperationException($"无法反序列化条件 TypeId: {typeID}");

        if (string.IsNullOrEmpty(condition.Name))
            condition.Name = condition.GetDefaultName();

        return condition;
    }

    internal static ConditionDetectType ReadConditionKind(JToken? token)
    {
        if (token?.Type == JTokenType.String)
        {
            var text = token.Value<string>();
            if (string.Equals(text, nameof(ConditionDetectType.ActionCastStart), StringComparison.OrdinalIgnoreCase))
                return ConditionDetectType.ActionCast;
        }

        return ReadEnum(token, ConditionDetectType.Health);
    }

    internal static ActionReference ReadActionReference(JToken? token, JToken? legacyActionIDToken, JsonSerializer serializer)
    {
        if (token is not null)
            return ReadObject(token, serializer, new ActionReference());

        return new ActionReference
        {
            ActionID = ReadUInt(legacyActionIDToken)
        };
    }

    internal static float ReadFloat(JToken? token) =>
        token is null
            ? 0f
            : token.Type switch
            {
                JTokenType.Integer                                                          => token.Value<float>(),
                JTokenType.Float                                                            => token.Value<float>(),
                JTokenType.String when float.TryParse(token.Value<string>(), out var value) => value,
                _                                                                           => 0f
            };

    internal static uint ReadUInt(JToken? token) => (uint)Math.Max(0, (int)ReadFloat(token));

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
