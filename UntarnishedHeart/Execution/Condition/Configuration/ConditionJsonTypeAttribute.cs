using UntarnishedHeart.Execution.Condition.Enums;

namespace UntarnishedHeart.Execution.Condition.Configuration;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
internal sealed class ConditionJsonTypeAttribute
(
    string              typeID,
    ConditionDetectType kind
) : Attribute
{
    public string TypeID { get; } = typeID;

    public ConditionDetectType Kind { get; } = kind;
}
