using System.Reflection;
using UntarnishedHeart.Execution.Condition.Enums;
using UntarnishedHeart.Internal.Configuration.Json;

namespace UntarnishedHeart.Execution.Condition.Configuration;

internal sealed class ConditionJsonTypeRegistry : PolymorphicJsonTypeRegistry<ConditionBase, ConditionDetectType, ConditionJsonTypeAttribute>
{
    internal static ConditionJsonTypeRegistry Instance { get; } = new();

    protected override string DisplayName => "条件";

    protected override ConditionJsonTypeAttribute? GetMetadata(Type type) => type.GetCustomAttribute<ConditionJsonTypeAttribute>(false);

    protected override bool ShouldRegisterType(Type type) =>
        !string.Equals(type.Namespace, "UntarnishedHeart.Execution.Condition.Legacy", StringComparison.Ordinal);

    protected override string GetTypeID(ConditionJsonTypeAttribute metadata) => metadata.TypeID;

    protected override ConditionDetectType GetKind(ConditionJsonTypeAttribute metadata) => metadata.Kind;

    protected override void InitializeDefaultInstance(ConditionBase instance) => instance.ResetMetadata();
}
