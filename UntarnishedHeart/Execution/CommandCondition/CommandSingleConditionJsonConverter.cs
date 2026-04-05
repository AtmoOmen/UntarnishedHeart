using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UntarnishedHeart.Execution.CommandCondition.Configuration;
using UntarnishedHeart.Execution.CommandCondition.Enums;
using UntarnishedHeart.Execution.CommandCondition.Legacy;

namespace UntarnishedHeart.Execution.CommandCondition;

public sealed class CommandSingleConditionJsonConverter : JsonConverter<CommandSingleCondition>
{
    public override void WriteJson(JsonWriter writer, CommandSingleCondition? value, JsonSerializer serializer)
    {
        if (value is null)
        {
            writer.WriteNull();
            return;
        }

        if (value is LegacyCommandSingleCondition legacy)
            value = CommandSingleCondition.MigrateLegacyV1ToV2(legacy.DetectType, legacy.ComparisonType, legacy.TargetType, legacy.Value);

        SerializeToJObject(value).WriteTo(writer);
    }

    public override CommandSingleCondition? ReadJson
    (
        JsonReader              reader,
        Type                    objectType,
        CommandSingleCondition? existingValue,
        bool                    hasExistingValue,
        JsonSerializer          serializer
    )
    {
        if (reader.TokenType == JsonToken.Null)
            return null;

        var token = JToken.Load(reader);
        if (token.Type != JTokenType.Object)
            return null;

        var obj = CommandSingleConditionJSONMigrator.Instance.MigrateToLatest((JObject)token);
        return DeserializeCurrent(obj);
    }

    internal static JObject SerializeToJObject(CommandSingleCondition value)
    {
        if (value is LegacyCommandSingleCondition legacy)
            value = CommandSingleCondition.MigrateLegacyV1ToV2(legacy.DetectType, legacy.ComparisonType, legacy.TargetType, legacy.Value);

        var obj = new JObject
        {
            ["Version"] = CommandSingleConditionJSONMigrator.CurrentJSONVersion,
            ["Kind"]    = value.Kind.ToString()
        };

        switch (value)
        {
            case HealthCommandCondition health:
                obj["ComparisonType"] = health.ComparisonType.ToString();
                obj["TargetType"]     = health.TargetType.ToString();
                obj["Threshold"]      = health.Threshold;
                break;

            case StatusCommandCondition status:
                obj["ComparisonType"] = status.ComparisonType.ToString();
                obj["TargetType"]     = status.TargetType.ToString();
                obj["StatusID"]       = status.StatusID;
                break;

            case ActionCooldownCommandCondition cooldown:
                obj["ComparisonType"] = cooldown.ComparisonType.ToString();
                obj["ActionID"]       = cooldown.ActionID;
                break;

            case ActionCastStartCommandCondition castStart:
                obj["ActionID"] = castStart.ActionID;
                break;
        }

        return obj;
    }

    internal static CommandSingleCondition DeserializeCurrent(JObject obj)
    {
        var kind = ReadEnum(obj["Kind"], CommandDetectType.Health);

        return kind switch
        {
            CommandDetectType.Health => new HealthCommandCondition
            {
                ComparisonType = ReadEnum(obj["ComparisonType"], NumericComparisonType.LessThan),
                TargetType     = ReadEnum(obj["TargetType"],     CommandTargetType.Target),
                Threshold      = ReadFloat(obj["Threshold"])
            },
            CommandDetectType.Status => new StatusCommandCondition
            {
                ComparisonType = ReadEnum(obj["ComparisonType"], PresenceComparisonType.Has),
                TargetType     = ReadEnum(obj["TargetType"],     CommandTargetType.Target),
                StatusID       = ReadUInt(obj["StatusID"] ?? obj["StatusId"])
            },
            CommandDetectType.ActionCooldown => new ActionCooldownCommandCondition
            {
                ComparisonType = ReadEnum(obj["ComparisonType"], CooldownComparisonType.Finished),
                ActionID       = ReadUInt(obj["ActionID"] ?? obj["ActionId"])
            },
            CommandDetectType.ActionCastStart => new ActionCastStartCommandCondition
            {
                ActionID = ReadUInt(obj["ActionID"] ?? obj["ActionId"])
            },
            _ => new HealthCommandCondition()
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
            var raw = token.Value<int>();
            if (Enum.IsDefined(typeof(TEnum), raw))
                return (TEnum)Enum.ToObject(typeof(TEnum), raw);
        }

        if (token.Type == JTokenType.String && Enum.TryParse<TEnum>(token.Value<string>(), true, out var value))
            return value;

        return fallback;
    }
}
