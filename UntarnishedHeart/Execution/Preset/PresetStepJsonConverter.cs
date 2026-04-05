using Dalamud.Game.ClientState.Objects.Enums;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UntarnishedHeart.Execution.Condition;
using UntarnishedHeart.Execution.Enums;
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
        JsonReader       reader,
        Type             objectType,
        PresetStep?      existingValue,
        bool             hasExistingValue,
        JsonSerializer   serializer
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
            ["Version"]                  = PresetStepJSONMigrator.CurrentJSONVersion,
            ["Note"]                     = value.Note,
            ["StopWhenBusy"]             = value.StopWhenBusy,
            ["StopInCombat"]             = value.StopInCombat,
            ["StopWhenAnyAlive"]         = value.StopWhenAnyAlive,
            ["Position"]                 = JToken.FromObject(value.Position, serializer),
            ["MoveType"]                 = JToken.FromObject(value.MoveType, serializer),
            ["WaitForGetClose"]          = value.WaitForGetClose,
            ["DataID"]                   = value.DataID,
            ["ObjectKind"]               = JToken.FromObject(value.ObjectKind, serializer),
            ["WaitForTargetSpawn"]       = value.WaitForTargetSpawn,
            ["WaitForTarget"]            = value.WaitForTarget,
            ["TargetNeedTargetable"]     = value.TargetNeedTargetable,
            ["InteractWithTarget"]       = value.InteractWithTarget,
            ["InteractNeedTargetAnything"] = value.InteractNeedTargetAnything,
            ["InteractWithNearestObject"]  = value.InteractWithNearestObject,
            ["Commands"]                 = value.Commands,
            ["Condition"]                = JToken.FromObject(value.Condition, serializer),
            ["Delay"]                    = value.Delay,
            ["JumpToIndex"]              = value.JumpToIndex
        };

    internal static PresetStep DeserializeCurrent(JObject jsonObject, JsonSerializer serializer) =>
        new()
        {
            Note                       = ReadString(jsonObject["Note"]),
            StopWhenBusy               = ReadBool(jsonObject["StopWhenBusy"]),
            StopInCombat               = ReadBool(jsonObject["StopInCombat"], true),
            StopWhenAnyAlive           = ReadBool(jsonObject["StopWhenAnyAlive"]),
            Position                   = ReadObject(jsonObject["Position"], serializer, default(System.Numerics.Vector3)),
            MoveType                   = ReadEnum(jsonObject["MoveType"], MoveType.传送),
            WaitForGetClose            = ReadBool(jsonObject["WaitForGetClose"]),
            DataID                     = ReadUInt(jsonObject["DataID"]),
            ObjectKind                 = ReadEnum(jsonObject["ObjectKind"], ObjectKind.BattleNpc),
            WaitForTargetSpawn         = ReadBool(jsonObject["WaitForTargetSpawn"]),
            WaitForTarget              = ReadBool(jsonObject["WaitForTarget"], true),
            TargetNeedTargetable       = ReadBool(jsonObject["TargetNeedTargetable"], true),
            InteractWithTarget         = ReadBool(jsonObject["InteractWithTarget"]),
            InteractNeedTargetAnything = ReadBool(jsonObject["InteractNeedTargetAnything"], true),
            InteractWithNearestObject  = ReadBool(jsonObject["InteractWithNearestObject"]),
            Commands                   = ReadString(jsonObject["Commands"]),
            Condition                  = ReadObject(jsonObject["Condition"], serializer, new ConditionCollection()),
            Delay                      = ReadUInt(jsonObject["Delay"], 5000),
            JumpToIndex                = ReadInt(jsonObject["JumpToIndex"], -1)
        };

    internal static bool ReadBool(JToken? token, bool fallback = false) =>
        token is null
            ? fallback
            : token.Type switch
            {
                JTokenType.Boolean                                                       => token.Value<bool>(),
                JTokenType.Integer                                                       => token.Value<int>() != 0,
                JTokenType.String when bool.TryParse(token.Value<string>(), out var value) => value,
                _                                                                        => fallback
            };

    internal static int ReadInt(JToken? token, int fallback = 0) =>
        token is null
            ? fallback
            : token.Type switch
            {
                JTokenType.Integer                                                     => token.Value<int>(),
                JTokenType.Float                                                       => (int)token.Value<float>(),
                JTokenType.String when int.TryParse(token.Value<string>(), out var value) => value,
                _                                                                      => fallback
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
