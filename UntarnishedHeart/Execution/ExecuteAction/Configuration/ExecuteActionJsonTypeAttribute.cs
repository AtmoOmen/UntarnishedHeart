using UntarnishedHeart.Execution.ExecuteAction.Enums;

namespace UntarnishedHeart.Execution.ExecuteAction.Configuration;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
internal sealed class ExecuteActionJsonTypeAttribute
(
    string            typeID,
    ExecuteActionKind kind
) : Attribute
{
    public string TypeID { get; } = typeID;

    public ExecuteActionKind Kind { get; } = kind;
}
