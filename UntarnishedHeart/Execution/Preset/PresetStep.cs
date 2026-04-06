using Newtonsoft.Json;
using UntarnishedHeart.Execution.ExecuteAction;

namespace UntarnishedHeart.Execution.Preset;

[JsonConverter(typeof(PresetStepJsonConverter))]
public class PresetStep : IEquatable<PresetStep>
{
    public string Name { get; set; } = string.Empty;

    public string Remark { get; set; } = string.Empty;

    public List<ExecuteActionBase> EnterActions { get; set; } = [];

    public List<ExecuteActionBase> BodyActions { get; set; } = [];

    public List<ExecuteActionBase> ExitActions { get; set; } = [];

    public bool Equals(PresetStep? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;

        return Name   == other.Name                           &&
               Remark == other.Remark                         &&
               EnterActions.SequenceEqual(other.EnterActions) &&
               BodyActions.SequenceEqual(other.BodyActions)   &&
               ExitActions.SequenceEqual(other.ExitActions);
    }

    public override string ToString() =>
        $"ExecutorPresetStep_{Name}_{EnterActions.Count}_{BodyActions.Count}_{ExitActions.Count}";

    public override bool Equals(object? obj) => Equals(obj as PresetStep);

    public override int GetHashCode() => HashCode.Combine(Name, Remark, EnterActions.Count, BodyActions.Count, ExitActions.Count);

    public static PresetStep Copy(PresetStep source) =>
        new()
        {
            Name         = source.Name,
            Remark       = source.Remark,
            EnterActions = source.EnterActions.Select(ExecuteActionBase.Copy).ToList(),
            BodyActions  = source.BodyActions.Select(ExecuteActionBase.Copy).ToList(),
            ExitActions  = source.ExitActions.Select(ExecuteActionBase.Copy).ToList()
        };
}
