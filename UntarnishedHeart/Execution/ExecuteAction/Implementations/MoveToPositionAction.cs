using System.Numerics;
using Newtonsoft.Json;
using UntarnishedHeart.Execution.Condition;
using UntarnishedHeart.Execution.Enums;
using UntarnishedHeart.Execution.ExecuteAction.Configuration;
using UntarnishedHeart.Execution.ExecuteAction.Enums;
using UntarnishedHeart.Execution.ExecuteAction.Helpers;
using UntarnishedHeart.Windows;

namespace UntarnishedHeart.Execution.ExecuteAction.Implementations;

[JsonObject(MemberSerialization.OptIn)]
[ExecuteActionJsonType("MoveToPosition", ExecuteActionKind.MoveToPosition)]
public sealed class MoveToPositionAction : ExecuteActionBase
{
    [JsonProperty("Position")]
    public Vector3 Position { get; set; }

    [JsonProperty("MoveType")]
    public MoveType MoveType { get; set; } = MoveType.传送;

    public override ExecuteActionKind Kind => ExecuteActionKind.MoveToPosition;

    public override void Draw()
    {
        ImGui.SetNextItemWidth(240f * GlobalUIScale);
        var moveTypeCandidates = Enum.GetValues<MoveType>();
        using (var combo = ImRaii.Combo("移动方式###MoveTypeCombo", MoveType.GetDescription(), ImGuiComboFlags.HeightLargest))
        {
            if (combo)
                ImGui.CloseCurrentPopup();
        }

        if (ImGui.IsItemClicked())
        {
            CollectionSelectorWindow.OpenEnum
            (
                "选择移动方式",
                "暂无可选移动方式",
                MoveType,
                value => MoveType = value,
                moveTypeCandidates
            );
        }

        var position = Position;
        ImGui.SetNextItemWidth(240f * GlobalUIScale);
        if (ImGui.InputFloat3("位置###MovePositionInput", ref position))
            Position = position;

        ExecuteActionDrawHelper.DrawPositionSelector("MoveGetPosition", currentPosition => Position = currentPosition, () => Position);
    }

    protected override bool EqualsCore(ExecuteActionBase other) =>
        other is MoveToPositionAction action &&
        Position.Equals(action.Position)     &&
        MoveType == action.MoveType;

    protected override int GetCoreHashCode() => HashCode.Combine(Position, (int)MoveType);

    public override ExecuteActionBase DeepCopy() =>
        CopyBasePropertiesTo
        (
            new MoveToPositionAction
            {
                Position = Position,
                MoveType = MoveType
            }
        );
}
