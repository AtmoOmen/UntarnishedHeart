using Newtonsoft.Json;
using UntarnishedHeart.Execution.Condition;
using UntarnishedHeart.Execution.ExecuteAction.Configuration;
using UntarnishedHeart.Execution.ExecuteAction.Enums;

namespace UntarnishedHeart.Execution.ExecuteAction;

[JsonConverter(typeof(ExecuteActionJSONConverter))]
public abstract class ExecuteActionBase : IEquatable<ExecuteActionBase>
{
    public string Name { get; set; } = string.Empty;

    public string Remark { get; set; } = string.Empty;

    public ConditionCollection Condition { get; set; } = new();

    public abstract ExecuteActionKind Kind { get; }

    public abstract void Draw();

    public abstract ExecuteActionBase DeepCopy();

    public string GetDefaultName() => Kind.GetDescription();

    public void ResetMetadata()
    {
        Name   = GetDefaultName();
        Remark = string.Empty;
    }

    public bool Equals(ExecuteActionBase? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;

        return Kind == other.Kind                  &&
               Name == other.Name                 &&
               Remark == other.Remark             &&
               EqualsCore(other)                  &&
               Condition.Equals(other.Condition);
    }

    protected abstract bool EqualsCore(ExecuteActionBase other);

    public override bool Equals(object? obj) => Equals(obj as ExecuteActionBase);

    public override int GetHashCode() => HashCode.Combine((int)Kind, Name, Remark, GetCoreHashCode(), Condition);

    protected abstract int GetCoreHashCode();

    protected T CopyBasePropertiesTo<T>(T target)
        where T : ExecuteActionBase
    {
        target.Name      = Name;
        target.Remark    = Remark;
        target.Condition = ConditionCollection.Copy(Condition);
        return target;
    }

    public static ExecuteActionBase Copy(ExecuteActionBase source) => source.DeepCopy();
}
