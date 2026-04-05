using UntarnishedHeart.Execution.Condition;
using UntarnishedHeart.Execution.ExecuteAction.Enums;

namespace UntarnishedHeart.Execution.ExecuteAction.Implementations;

public sealed class TextCommandAction : ExecuteAction
{
    public string Commands { get; set; } = string.Empty;

    public override ExecuteActionKind Kind => ExecuteActionKind.TextCommand;

    protected override bool EqualsCore(ExecuteAction other) => other is TextCommandAction action && Commands == action.Commands;

    protected override int GetCoreHashCode() => Commands.GetHashCode(StringComparison.Ordinal);

    public override ExecuteAction DeepCopy() =>
        new TextCommandAction
        {
            Commands  = Commands,
            Condition = ConditionCollection.Copy(Condition)
        };
}
