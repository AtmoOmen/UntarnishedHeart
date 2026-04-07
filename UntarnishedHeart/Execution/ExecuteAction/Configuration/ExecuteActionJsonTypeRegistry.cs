using System.Reflection;
using UntarnishedHeart.Execution.ExecuteAction.Enums;
using UntarnishedHeart.Internal.Configuration.Json;

namespace UntarnishedHeart.Execution.ExecuteAction.Configuration;

internal sealed class ExecuteActionJsonTypeRegistry : PolymorphicJsonTypeRegistry<ExecuteActionBase, ExecuteActionKind, ExecuteActionJsonTypeAttribute>
{
    internal static ExecuteActionJsonTypeRegistry Instance { get; } = new();

    protected override string DisplayName => "执行动作";

    protected override ExecuteActionJsonTypeAttribute? GetMetadata(Type type) => type.GetCustomAttribute<ExecuteActionJsonTypeAttribute>(false);

    protected override string GetTypeID(ExecuteActionJsonTypeAttribute metadata) => metadata.TypeID;

    protected override ExecuteActionKind GetKind(ExecuteActionJsonTypeAttribute metadata) => metadata.Kind;

    protected override void InitializeDefaultInstance(ExecuteActionBase instance) => instance.ResetMetadata();
}
