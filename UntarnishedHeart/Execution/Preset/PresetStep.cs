using Newtonsoft.Json;
using UntarnishedHeart.Execution.ExecuteAction;

namespace UntarnishedHeart.Execution.Preset;

[JsonConverter(typeof(PresetStepJsonConverter))]
public class PresetStep : IEquatable<PresetStep>
{
    public string Name { get; set; } = string.Empty;

    public string Remark { get; set; } = string.Empty;

    public List<ExecuteActionBase> Actions { get; set; } = [];

    public bool Equals(PresetStep? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;

        return Name    == other.Name    &&
               Remark  == other.Remark  &&
               Actions.SequenceEqual(other.Actions);
    }

    public override string ToString() =>
        $"ExecutorPresetStep_{Name}_{Actions.Count}";

    public override bool Equals(object? obj) => Equals(obj as PresetStep);

    public override int GetHashCode() => HashCode.Combine(Name, Remark, Actions.Count);

    public static PresetStep Copy(PresetStep source) =>
        new()
        {
            Name    = source.Name,
            Remark  = source.Remark,
            Actions = source.Actions.Select(ExecuteActionBase.Copy).ToList()
        };
}
