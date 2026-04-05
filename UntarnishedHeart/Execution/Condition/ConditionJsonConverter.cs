using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UntarnishedHeart.Execution.Condition.Configuration;
using UntarnishedHeart.Execution.Condition.Enums;
using UntarnishedHeart.Execution.Condition.Legacy;

namespace UntarnishedHeart.Execution.Condition;

public sealed class ConditionJsonConverter : JsonConverter<Condition>
{
    public override void WriteJson(JsonWriter writer, Condition? value, JsonSerializer serializer)
    {
        if (value is null)
        {
            writer.WriteNull();
            return;
        }

        if (value is LegacyCondition legacy)
            value = Condition.MigrateLegacyV1ToV2(legacy.DetectType, legacy.ComparisonType, legacy.TargetType, legacy.Value);

        SerializeToJObject(value).WriteTo(writer);
    }

    public override Condition? ReadJson
    (
        JsonReader              reader,
        Type                    objectType,
        Condition? existingValue,
        bool                    hasExistingValue,
        JsonSerializer          serializer
    )
    {
        if (reader.TokenType == JsonToken.Null)
            return null;

        var token = JToken.Load(reader);
        if (token.Type != JTokenType.Object)
            return null;

        var obj = ConditionJSONMigrator.Instance.MigrateToLatest((JObject)token);
        return DeserializeCurrent(obj);
    }

    internal static JObject SerializeToJObject(Condition value)
    {
        if (value is LegacyCondition legacy)
            value = Condition.MigrateLegacyV1ToV2(legacy.DetectType, legacy.ComparisonType, legacy.TargetType, legacy.Value);

        var obj = new JObject
        {
            ["Version"] = ConditionJSONMigrator.CurrentJSONVersion,
            ["Kind"]    = value.Kind.ToString()
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
                obj["StatusID"]       = status.StatusID;
                break;

            case ActionCooldownCondition cooldown:
                obj["ComparisonType"] = cooldown.ComparisonType.ToString();
                obj["ActionID"]       = cooldown.ActionID;
                break;

            case ActionCastStartCondition castStart:
                obj["ActionID"] = castStart.ActionID;
                break;
        }

        return obj;
    }

    internal static Condition DeserializeCurrent(JObject obj)
    {
        var kind = ReadEnum(obj["Kind"], ConditionDetectType.Health);

        return kind switch
        {
            ConditionDetectType.Health => new HealthCondition
            {
                ComparisonType = ReadEnum(obj["ComparisonType"], NumericComparisonType.LessThan),
                TargetType     = ReadEnum(obj["TargetType"],     ConditionTargetType.Target),
                Threshold      = ReadFloat(obj["Threshold"])
            },
            ConditionDetectType.Status => new StatusCondition
            {
                ComparisonType = ReadEnum(obj["ComparisonType"], PresenceComparisonType.Has),
                TargetType     = ReadEnum(obj["TargetType"],     ConditionTargetType.Target),
                StatusID       = ReadUInt(obj["StatusID"] ?? obj["StatusId"])
            },
            ConditionDetectType.ActionCooldown => new ActionCooldownCondition
            {
                ComparisonType = ReadEnum(obj["ComparisonType"], CooldownComparisonType.Finished),
                ActionID       = ReadUInt(obj["ActionID"] ?? obj["ActionId"])
            },
            ConditionDetectType.ActionCastStart => new ActionCastStartCondition
            {
                ActionID = ReadUInt(obj["ActionID"] ?? obj["ActionId"])
            },
            _ => new HealthCondition()
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
