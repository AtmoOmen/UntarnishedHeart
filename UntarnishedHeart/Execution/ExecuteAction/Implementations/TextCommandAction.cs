using UntarnishedHeart.Execution.Condition;
using UntarnishedHeart.Execution.ExecuteAction.Enums;

namespace UntarnishedHeart.Execution.ExecuteAction.Implementations;

public sealed class TextCommandAction : ExecuteActionBase
{
    public string Commands { get; set; } = string.Empty;

    public override ExecuteActionKind Kind => ExecuteActionKind.TextCommand;

    public override void Draw()
    {
        var commands    = Commands;
        var inputHeight = Math.Max(ImGui.GetTextLineHeightWithSpacing() * 6f, 120f * GlobalUIScale);
        if (ImGui.InputTextMultiline("###CommandsInput", ref commands, 4096, new(-1f, inputHeight)))
            Commands = commands;
    }

    protected override bool EqualsCore(ExecuteActionBase other) => other is TextCommandAction action && Commands == action.Commands;

    protected override int GetCoreHashCode() => Commands.GetHashCode(StringComparison.Ordinal);

    public override ExecuteActionBase DeepCopy() =>
        new TextCommandAction
        {
            Commands  = Commands,
            Condition = ConditionCollection.Copy(Condition)
        };
}
