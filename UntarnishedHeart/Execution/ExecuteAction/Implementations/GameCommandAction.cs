using Dalamud.Utility;
using Newtonsoft.Json;
using OmenTools.Info.Game.Enums;
using UntarnishedHeart.Execution.ExecuteAction.Configuration;
using UntarnishedHeart.Execution.ExecuteAction.Enums;
using UntarnishedHeart.Windows;

namespace UntarnishedHeart.Execution.ExecuteAction.Implementations;

[JsonObject(MemberSerialization.OptIn)]
[ExecuteActionJsonType("GameCommand", ExecuteActionKind.GameCommand)]
public sealed class GameCommandAction : ExecuteActionBase
{
    private const string COMMAND_DOC_LINK    = "https://github.com/AtmoOmen/OmenTools/blob/main/Info/Game/Enums/ExecuteCommandFlag.cs";
    private const string COMMAND_DOC_TOOLTIP = "各游戏命令详情与参数说明见代码注释";

    [JsonProperty("Command")]
    public ExecuteCommandFlag Command { get; set; }

    [JsonProperty("Param1")]
    public uint Param1 { get; set; }

    [JsonProperty("Param2")]
    public uint Param2 { get; set; }

    [JsonProperty("Param3")]
    public uint Param3 { get; set; }

    [JsonProperty("Param4")]
    public uint Param4 { get; set; }

    public override ExecuteActionKind Kind => ExecuteActionKind.GameCommand;

    public override void Draw()
    {
        ImGui.SetNextItemWidth(240f * GlobalUIScale);
        var commandCandidates = Enum.GetValues<ExecuteCommandFlag>();

        using (var combo = ImRaii.Combo("命令###GameCommand", GetCommandDisplayText(), ImGuiComboFlags.HeightLargest))
        {
            if (combo)
                ImGui.CloseCurrentPopup();
        }

        var commandComboClicked = ImGui.IsItemClicked();

        ImGui.SameLine();
        if (ImGuiOm.ButtonIcon("GameCommandDoc", FontAwesomeIcon.Link, COMMAND_DOC_TOOLTIP, true))
            Util.OpenLink(COMMAND_DOC_LINK);

        if (commandComboClicked)
        {
            CollectionSelectorWindow.OpenEnum
            (
                "选择游戏命令",
                "暂无可选游戏命令",
                Command,
                value => Command = value,
                commandCandidates
            );
        }

        var commandValue = (uint)Command;
        ImGui.SetNextItemWidth(240f * GlobalUIScale);
        if (ImGui.InputUInt("命令值###GameCommandValue", ref commandValue))
            Command = (ExecuteCommandFlag)commandValue;

        ImGui.SetNextItemWidth(240f * GlobalUIScale);
        var param1 = Param1;
        if (ImGui.InputUInt("参数 1###GameCommandParam1", ref param1))
            Param1 = param1;

        ImGui.SetNextItemWidth(240f * GlobalUIScale);
        var param2 = Param2;
        if (ImGui.InputUInt("参数 2###GameCommandParam2", ref param2))
            Param2 = param2;

        ImGui.SetNextItemWidth(240f * GlobalUIScale);
        var param3 = Param3;
        if (ImGui.InputUInt("参数 3###GameCommandParam3", ref param3))
            Param3 = param3;

        ImGui.SetNextItemWidth(240f * GlobalUIScale);
        var param4 = Param4;
        if (ImGui.InputUInt("参数 4###GameCommandParam4", ref param4))
            Param4 = param4;
    }

    protected override bool EqualsCore(ExecuteActionBase other) =>
        other is GameCommandAction action &&
        Command == action.Command         &&
        Param1  == action.Param1          &&
        Param2  == action.Param2          &&
        Param3  == action.Param3          &&
        Param4  == action.Param4;

    protected override int GetCoreHashCode() => HashCode.Combine((int)Command, Param1, Param2, Param3, Param4);

    private string GetCommandDisplayText() =>
        Enum.IsDefined(typeof(ExecuteCommandFlag), Command)
            ? Command.GetDescription()
            : $"未知命令 ({(uint)Command})";

    public override ExecuteActionBase DeepCopy() =>
        CopyBasePropertiesTo
        (
            new GameCommandAction
            {
                Command = Command,
                Param1  = Param1,
                Param2  = Param2,
                Param3  = Param3,
                Param4  = Param4
            }
        );
}
