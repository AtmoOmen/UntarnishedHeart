using Dalamud.Game.ClientState.Conditions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UntarnishedHeart.Execution.Condition.Enums;
using UntarnishedHeart.Execution.Models;
using UntarnishedHeart.Execution.Preset.Helpers;

namespace UntarnishedHeart.Execution.Condition.Configuration.Migrators;

public sealed class ConditionJsonConverter : JsonConverter<ConditionBase>
{
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
        return DeserializeCurrent(jsonObject, serializer);
    }

    internal static JObject SerializeToJObject(ConditionBase value, JsonSerializer serializer)
    {
        var obj = new JObject
        {
            ["Version"] = ConditionJSONMigrator.CurrentJSONVersion,
            ["Kind"]    = value.Kind.ToString(),
            ["Name"]    = value.Name,
            ["Remark"]  = value.Remark
        };

        switch (value)
        {
            case GameConditionStateCondition gameCondition:
                obj["Flag"]           = gameCondition.Flag.ToString();
                obj["ComparisonType"] = gameCondition.ComparisonType.ToString();
                break;

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

            case ActionUsableCondition usable:
                obj["ComparisonType"] = usable.ComparisonType.ToString();
                obj["Action"]         = JToken.FromObject(usable.Action, serializer);
                break;

            case PositionRangeCondition positionRange:
                obj["ComparisonType"] = positionRange.ComparisonType.ToString();
                obj["Range"]          = JToken.FromObject(positionRange.Range, serializer);
                break;

            case NearbyTargetCondition nearbyTarget:
                obj["ComparisonType"] = nearbyTarget.ComparisonType.ToString();
                obj["Selector"]       = JToken.FromObject(nearbyTarget.Selector, serializer);
                break;

            case HasTargetCondition hasTarget:
                obj["ComparisonType"] = hasTarget.ComparisonType.ToString();
                break;

            case HasSpecificTargetCondition hasSpecificTarget:
                obj["ComparisonType"] = hasSpecificTarget.ComparisonType.ToString();
                obj["Selector"]       = JToken.FromObject(hasSpecificTarget.Selector, serializer);
                break;

            case PartyAllDeadCondition partyAllDead:
                obj["ComparisonType"] = partyAllDead.ComparisonType.ToString();
                break;

            case TargetTargetIsSelfCondition targetTargetIsSelf:
                obj["ComparisonType"] = targetTargetIsSelf.ComparisonType.ToString();
                break;

            case PlayerLevelCondition playerLevel:
                obj["ComparisonType"] = playerLevel.ComparisonType.ToString();
                obj["ExpectedValue"]  = playerLevel.ExpectedValue;
                break;

            case OptimalPartyRecommendationCondition optimalPartyRecommendation:
                obj["ComparisonType"] = optimalPartyRecommendation.ComparisonType.ToString();
                obj["ExpectedValue"]  = optimalPartyRecommendation.ExpectedValue;
                break;

            case CompletedDutyCountCondition completedDutyCount:
                obj["ComparisonType"] = completedDutyCount.ComparisonType.ToString();
                obj["ExpectedValue"]  = completedDutyCount.ExpectedValue;
                break;

            case AchievementCountCondition achievementCount:
                obj["ComparisonType"] = achievementCount.ComparisonType.ToString();
                obj["ExpectedValue"]  = achievementCount.ExpectedValue;
                obj["AchievementId"]  = achievementCount.AchievementID;
                break;

            case ItemCountCondition itemCount:
                obj["ComparisonType"] = itemCount.ComparisonType.ToString();
                obj["ExpectedValue"]  = itemCount.ExpectedValue;
                obj["ItemId"]         = itemCount.ItemID;
                break;
        }

        return obj;
    }

    internal static ConditionBase DeserializeCurrent(JObject obj, JsonSerializer serializer)
    {
        var kind   = ReadConditionKind(obj["Kind"]);
        var name   = ReadString(obj["Name"], kind.GetDescription());
        var remark = ReadString(obj["Remark"]);

        return kind switch
        {
            ConditionDetectType.GameCondition => PopulateCommonFields
            (
                new GameConditionStateCondition
                {
                    Flag           = ReadEnum(obj["Flag"],           ConditionFlag.InCombat),
                    ComparisonType = ReadEnum(obj["ComparisonType"], PresenceComparisonType.NotHas)
                },
                name,
                remark
            ),
            ConditionDetectType.Health => PopulateCommonFields
            (
                new HealthCondition
                {
                    ComparisonType = ReadEnum(obj["ComparisonType"], NumericComparisonType.LessThan),
                    TargetType     = ReadEnum(obj["TargetType"],     ConditionTargetType.Target),
                    Threshold      = ReadFloat(obj["Threshold"])
                },
                name,
                remark
            ),
            ConditionDetectType.Status => PopulateCommonFields
            (
                new StatusCondition
                {
                    ComparisonType = ReadEnum(obj["ComparisonType"], PresenceComparisonType.Has),
                    TargetType     = ReadEnum(obj["TargetType"],     ConditionTargetType.Target),
                    StatusID       = ReadUInt(obj["StatusId"] ?? obj["StatusID"])
                },
                name,
                remark
            ),
            ConditionDetectType.ActionCast or ConditionDetectType.ActionCastStart => PopulateCommonFields
            (
                new ActionCastCondition
                {
                    ComparisonType = ReadEnum(obj["ComparisonType"], PresenceComparisonType.Has),
                    TargetType     = ReadEnum(obj["TargetType"],     ConditionTargetType.Target),
                    Action         = ReadActionReference(obj["Action"], obj["ActionId"] ?? obj["ActionID"], serializer)
                },
                name,
                remark
            ),
            ConditionDetectType.ActionCooldown => PopulateCommonFields
            (
                new ActionCooldownCondition
                {
                    ComparisonType = ReadEnum(obj["ComparisonType"], CooldownComparisonType.Finished),
                    Action         = ReadActionReference(obj["Action"], obj["ActionId"] ?? obj["ActionID"], serializer)
                },
                name,
                remark
            ),
            ConditionDetectType.ActionUsable => PopulateCommonFields
            (
                new ActionUsableCondition
                {
                    ComparisonType = ReadEnum(obj["ComparisonType"], PresenceComparisonType.Has),
                    Action         = ReadActionReference(obj["Action"], obj["ActionId"] ?? obj["ActionID"], serializer)
                },
                name,
                remark
            ),
            ConditionDetectType.PositionRange => PopulateCommonFields
            (
                new PositionRangeCondition
                {
                    ComparisonType = ReadEnum(obj["ComparisonType"], PresenceComparisonType.Has),
                    Range          = ReadObject(obj["Range"], serializer, new PositionRange())
                },
                name,
                remark
            ),
            ConditionDetectType.NearbyTarget => PopulateCommonFields
            (
                new NearbyTargetCondition
                {
                    ComparisonType = ReadEnum(obj["ComparisonType"], PresenceComparisonType.Has),
                    Selector       = ReadObject(obj["Selector"], serializer, new TargetSelector())
                },
                name,
                remark
            ),
            ConditionDetectType.HasTarget => PopulateCommonFields
            (
                new HasTargetCondition
                {
                    ComparisonType = ReadEnum(obj["ComparisonType"], PresenceComparisonType.Has)
                },
                name,
                remark
            ),
            ConditionDetectType.HasSpecificTarget => PopulateCommonFields
            (
                new HasSpecificTargetCondition
                {
                    ComparisonType = ReadEnum(obj["ComparisonType"], PresenceComparisonType.Has),
                    Selector       = ReadObject(obj["Selector"], serializer, new TargetSelector())
                },
                name,
                remark
            ),
            ConditionDetectType.PartyAllDead => PopulateCommonFields
            (
                new PartyAllDeadCondition
                {
                    ComparisonType = ReadEnum(obj["ComparisonType"], PresenceComparisonType.Has)
                },
                name,
                remark
            ),
            ConditionDetectType.TargetTargetIsSelf => PopulateCommonFields
            (
                new TargetTargetIsSelfCondition
                {
                    ComparisonType = ReadEnum(obj["ComparisonType"], PresenceComparisonType.Has)
                },
                name,
                remark
            ),
            ConditionDetectType.PlayerLevel => PopulateCommonFields
            (
                new PlayerLevelCondition
                {
                    ComparisonType = ReadEnum(obj["ComparisonType"], NumericComparisonType.EqualTo),
                    ExpectedValue  = ReadInt(obj["ExpectedValue"])
                },
                name,
                remark
            ),
            ConditionDetectType.OptimalPartyRecommendation => PopulateCommonFields
            (
                new OptimalPartyRecommendationCondition
                {
                    ComparisonType = ReadEnum(obj["ComparisonType"], NumericComparisonType.EqualTo),
                    ExpectedValue  = ReadInt(obj["ExpectedValue"])
                },
                name,
                remark
            ),
            ConditionDetectType.CompletedDutyCount => PopulateCommonFields
            (
                new CompletedDutyCountCondition
                {
                    ComparisonType = ReadEnum(obj["ComparisonType"], NumericComparisonType.EqualTo),
                    ExpectedValue  = ReadInt(obj["ExpectedValue"])
                },
                name,
                remark
            ),
            ConditionDetectType.AchievementCount => PopulateCommonFields
            (
                new AchievementCountCondition
                {
                    ComparisonType = ReadEnum(obj["ComparisonType"], NumericComparisonType.EqualTo),
                    ExpectedValue  = ReadInt(obj["ExpectedValue"]),
                    AchievementID  = ReadUInt(obj["AchievementId"] ?? obj["AchievementID"])
                },
                name,
                remark
            ),
            ConditionDetectType.ItemCount => PopulateCommonFields
            (
                new ItemCountCondition
                {
                    ComparisonType = ReadEnum(obj["ComparisonType"], NumericComparisonType.EqualTo),
                    ExpectedValue  = ReadInt(obj["ExpectedValue"]),
                    ItemID         = ReadUInt(obj["ItemId"] ?? obj["ItemID"])
                },
                name,
                remark
            ),
            _ => PopulateCommonFields(new HealthCondition(), name, remark)
        };
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

    private static T PopulateCommonFields<T>(T condition, string name, string remark)
        where T : ConditionBase
    {
        condition.Name   = name;
        condition.Remark = remark;
        return condition;
    }
}
