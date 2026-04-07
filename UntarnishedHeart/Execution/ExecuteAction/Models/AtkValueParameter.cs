using Newtonsoft.Json;
using OmenTools.Interop.Game.Models;
using UntarnishedHeart.Execution.ExecuteAction.Enums;

namespace UntarnishedHeart.Execution.ExecuteAction.Models;

[JsonObject(MemberSerialization.OptIn)]
public sealed class AtkValueParameter : IEquatable<AtkValueParameter>
{
    [JsonProperty("Type")]
    public AtkValueParameterType Type { get; set; }

    [JsonProperty("IntValue")]
    public int IntValue { get; set; }

    [JsonProperty("UIntValue")]
    public uint UIntValue { get; set; }

    [JsonProperty("FloatValue")]
    public float FloatValue { get; set; }

    [JsonProperty("BoolValue")]
    public bool BoolValue { get; set; }

    [JsonProperty("StringValue")]
    public string StringValue { get; set; } = string.Empty;

    public object ToValue() =>
        Type switch
        {
            AtkValueParameterType.Int    => IntValue,
            AtkValueParameterType.UInt   => UIntValue,
            AtkValueParameterType.Float  => FloatValue,
            AtkValueParameterType.Bool   => BoolValue,
            AtkValueParameterType.String => StringValue,
            _                            => IntValue
        };

    public static AtkValueArray CreateValueArray(IReadOnlyList<AtkValueParameter> parameters)
    {
        if (parameters.Count == 0)
            return new AtkValueArray();

        var values = new object[parameters.Count];
        for (var i = 0; i < parameters.Count; i++)
            values[i] = parameters[i].ToValue();

        return new AtkValueArray(values);
    }

    public AtkValueParameter DeepCopy() =>
        new()
        {
            Type        = Type,
            IntValue    = IntValue,
            UIntValue   = UIntValue,
            FloatValue  = FloatValue,
            BoolValue   = BoolValue,
            StringValue = StringValue
        };

    public bool Equals(AtkValueParameter? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;

        return Type      == other.Type             &&
               IntValue  == other.IntValue         &&
               UIntValue == other.UIntValue        &&
               FloatValue.Equals(other.FloatValue) &&
               BoolValue   == other.BoolValue      &&
               StringValue == other.StringValue;
    }

    public override bool Equals(object? obj) => Equals(obj as AtkValueParameter);

    public override int GetHashCode() => HashCode.Combine((int)Type, IntValue, UIntValue, FloatValue, BoolValue, StringValue);
}
