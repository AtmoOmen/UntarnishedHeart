using Newtonsoft.Json;
using UntarnishedHeart.Execution.Condition;
using UntarnishedHeart.Execution.ExecuteAction.Configuration;
using UntarnishedHeart.Execution.ExecuteAction.Enums;

namespace UntarnishedHeart.Execution.ExecuteAction;

[JsonConverter(typeof(PresetExecuteActionJsonConverter))]
public abstract class ExecuteAction : IEquatable<ExecuteAction>
{
    public ConditionCollection Condition { get; set; } = new();

    public abstract ExecuteActionKind Kind { get; }

    public abstract ExecuteAction DeepCopy();

    public bool Equals(ExecuteAction? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;

        return Kind == other.Kind && EqualsCore(other) && Condition.Equals(other.Condition);
    }

    protected abstract bool EqualsCore(ExecuteAction other);

    public override bool Equals(object? obj) => Equals(obj as ExecuteAction);

    public override int GetHashCode() => HashCode.Combine((int)Kind, GetCoreHashCode(), Condition);

    protected abstract int GetCoreHashCode();

    public static ExecuteAction Copy(ExecuteAction source) => source.DeepCopy();
}
