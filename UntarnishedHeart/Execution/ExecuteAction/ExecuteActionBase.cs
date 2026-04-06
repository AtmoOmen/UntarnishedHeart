using Newtonsoft.Json;
using UntarnishedHeart.Execution.Condition;
using UntarnishedHeart.Execution.ExecuteAction.Configuration;
using UntarnishedHeart.Execution.ExecuteAction.Enums;

namespace UntarnishedHeart.Execution.ExecuteAction;

[JsonConverter(typeof(ExecuteActionJsonConverter))]
public abstract class ExecuteActionBase : IEquatable<ExecuteActionBase>
{
    public ConditionCollection Condition { get; set; } = new();

    public abstract ExecuteActionKind Kind { get; }

    public abstract void Draw();

    public abstract ExecuteActionBase DeepCopy();

    public bool Equals(ExecuteActionBase? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;

        return Kind == other.Kind && EqualsCore(other) && Condition.Equals(other.Condition);
    }

    protected abstract bool EqualsCore(ExecuteActionBase other);

    public override bool Equals(object? obj) => Equals(obj as ExecuteActionBase);

    public override int GetHashCode() => HashCode.Combine((int)Kind, GetCoreHashCode(), Condition);

    protected abstract int GetCoreHashCode();

    public static ExecuteActionBase Copy(ExecuteActionBase source) => source.DeepCopy();
}
