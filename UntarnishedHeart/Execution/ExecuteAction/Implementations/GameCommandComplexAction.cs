using System.Numerics;
using Dalamud.Utility;
using Newtonsoft.Json;
using OmenTools.Info.Game.Enums;
using UntarnishedHeart.Execution.ExecuteAction.Configuration;
using UntarnishedHeart.Execution.ExecuteAction.Enums;
using UntarnishedHeart.Execution.ExecuteAction.Helpers;
using UntarnishedHeart.Windows;

namespace UntarnishedHeart.Execution.ExecuteAction.Implementations;

[JsonObject(MemberSerialization.OptIn)]
[ExecuteActionJsonType("GameCommandComplex", ExecuteActionKind.GameCommandComplex)]
public sealed class GameCommandComplexAction : ExecuteActionBase
{
    private const string COMMAND_DOC_LINK    = "https://github.com/AtmoOmen/OmenTools/blob/main/Info/Game/Enums/ExecuteCommandComplexFlag.cs";
    private const string COMMAND_DOC_TOOLTIP = "各游戏命令详情与参数说明见代码注释";

    [JsonProperty("Command")]
    public ExecuteCommandComplexFlag Command { get; set; }

    [JsonProperty("Target")]
    public uint Target { get; set; } = 0xE000_0000U;

    [JsonProperty("Location")]
    public Vector3 Location { get; set; }

    [JsonProperty("UseLocation")]
    public bool UseLocation { get; set; }

    [JsonProperty("Param1")]
    public uint Param1 { get; set; }

    [JsonProperty("Param2")]
    public uint Param2 { get; set; }

    [JsonProperty("Param3")]
    public uint Param3 { get; set; }

    [JsonProperty("Param4")]
    public uint Param4 { get; set; }

    public override ExecuteActionKind Kind => ExecuteActionKind.GameCommandComplex;

    public override void Draw()
    {
        ImGui.SetNextItemWidth(240f * GlobalUIScale);
        var commandCandidates = Enum.GetValues<ExecuteCommandComplexFlag>();

        using (var combo = ImRaii.Combo("命令###GameCommandComplex", GetCommandDisplayText(), ImGuiComboFlags.HeightLargest))
        {
            if (combo)
                ImGui.CloseCurrentPopup();
        }

        var commandComboClicked = ImGui.IsItemClicked();

        ImGui.SameLine();
        if (ImGuiOm.ButtonIcon("GameCommandComplexDoc", FontAwesomeIcon.Link, COMMAND_DOC_TOOLTIP, true))
            Util.OpenLink(COMMAND_DOC_LINK);

        if (commandComboClicked)
        {
            CollectionSelectorWindow.OpenEnum
            (
                "选择富参数游戏命令",
                "暂无可选富参数游戏命令",
                Command,
                value => Command = value,
                commandCandidates
            );
        }

        var commandValue = (uint)Command;
        ImGui.SetNextItemWidth(240f * GlobalUIScale);
        if (ImGui.InputUInt("命令值###GameCommandComplexValue", ref commandValue))
            Command = (ExecuteCommandComplexFlag)commandValue;

        var useLocation = UseLocation;
        if (ImGui.Checkbox("按坐标发送###GameCommandComplexUseLocation", ref useLocation))
            UseLocation = useLocation;

        if (UseLocation)
        {
            var location = Location;
            ImGui.SetNextItemWidth(240f * GlobalUIScale);
            if (ImGui.InputFloat3("坐标###GameCommandComplexLocation", ref location))
                Location = location;

            ExecuteActionDrawHelper.DrawPositionSelector("GameCommandComplexGetLocation", position => Location = position, () => Location);
        }
        else
        {
            ImGui.SetNextItemWidth(240f * GlobalUIScale);
            var target = Target;
            if (ImGui.InputUInt("目标 ID###GameCommandComplexTarget", ref target))
                Target = target;
        }

        ImGui.SetNextItemWidth(240f * GlobalUIScale);
        var param1 = Param1;
        if (ImGui.InputUInt("参数 1###GameCommandComplexParam1", ref param1))
            Param1 = param1;

        ImGui.SetNextItemWidth(240f * GlobalUIScale);
        var param2 = Param2;
        if (ImGui.InputUInt("参数 2###GameCommandComplexParam2", ref param2))
            Param2 = param2;

        ImGui.SetNextItemWidth(240f * GlobalUIScale);
        var param3 = Param3;
        if (ImGui.InputUInt("参数 3###GameCommandComplexParam3", ref param3))
            Param3 = param3;

        ImGui.SetNextItemWidth(240f * GlobalUIScale);
        var param4 = Param4;
        if (ImGui.InputUInt("参数 4###GameCommandComplexParam4", ref param4))
            Param4 = param4;
    }

    protected override bool EqualsCore(ExecuteActionBase other) =>
        other is GameCommandComplexAction action &&
        Command == action.Command                &&
        Target  == action.Target                 &&
        Location.Equals(action.Location)         &&
        UseLocation == action.UseLocation        &&
        Param1      == action.Param1             &&
        Param2      == action.Param2             &&
        Param3      == action.Param3             &&
        Param4      == action.Param4;

    protected override int GetCoreHashCode() => HashCode.Combine((int)Command, Target, Location, UseLocation, Param1, Param2, Param3, Param4);

    private string GetCommandDisplayText() =>
        Enum.IsDefined(typeof(ExecuteCommandComplexFlag), Command)
            ? Command.GetDescription()
            : $"未知命令 ({(uint)Command})";

    public override ExecuteActionBase DeepCopy() =>
        CopyBasePropertiesTo
        (
            new GameCommandComplexAction
            {
                Command     = Command,
                Target      = Target,
                Location    = Location,
                UseLocation = UseLocation,
                Param1      = Param1,
                Param2      = Param2,
                Param3      = Param3,
                Param4      = Param4
            }
        );
}
