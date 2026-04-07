using Newtonsoft.Json;
using UntarnishedHeart.Execution.ExecuteAction.Configuration;
using UntarnishedHeart.Execution.ExecuteAction.Enums;

namespace UntarnishedHeart.Execution.ExecuteAction.Implementations;

[JsonObject(MemberSerialization.OptIn)]
[ExecuteActionJsonType("TextCommand", ExecuteActionKind.TextCommand)]
public sealed class TextCommandAction : ExecuteActionBase
{
    [JsonProperty("Commands")]
    public string Commands { get; set; } = string.Empty;

    public override ExecuteActionKind Kind => ExecuteActionKind.TextCommand;

    public override void Draw()
    {
        var commands = Commands;
        if (ImGui.InputTextMultiline("###CommandsInput", ref commands, 4096, new(-1f, ImGui.GetTextLineHeightWithSpacing() * 6f)))
            Commands = commands;
    }

    protected override bool EqualsCore(ExecuteActionBase other) => other is TextCommandAction action && Commands == action.Commands;

    protected override int GetCoreHashCode() => Commands.GetHashCode(StringComparison.Ordinal);

    public override ExecuteActionBase DeepCopy() =>
        CopyBasePropertiesTo(new TextCommandAction { Commands = Commands });
}
