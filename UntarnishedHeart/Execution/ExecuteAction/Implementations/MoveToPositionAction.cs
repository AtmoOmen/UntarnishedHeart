using System.Numerics;
using UntarnishedHeart.Execution.Condition;
using UntarnishedHeart.Execution.Enums;
using UntarnishedHeart.Execution.ExecuteAction.Enums;
using UntarnishedHeart.Execution.ExecuteAction.Helpers;

namespace UntarnishedHeart.Execution.ExecuteAction.Implementations;

public sealed class MoveToPositionAction : ExecuteActionBase
{
    public Vector3 Position { get; set; }

    public MoveType MoveType { get; set; } = MoveType.传送;

    public override ExecuteActionKind Kind => ExecuteActionKind.MoveToPosition;

    public override void Draw()
    {
        ImGui.TextColored(KnownColor.LightSkyBlue.ToVector4(), "移动方式:");
        ImGui.SameLine();
        MoveType = ConditionBase.DrawEnumCombo("###MoveTypeCombo", MoveType);

        var position = Position;
        if (ImGui.InputFloat3("位置###MovePositionInput", ref position))
            Position = position;

        ExecuteActionDrawHelper.DrawCurrentPositionButton("MoveGetPosition", currentPosition => Position = currentPosition);
    }

    protected override bool EqualsCore(ExecuteActionBase other) =>
        other is MoveToPositionAction action &&
        Position.Equals(action.Position)     &&
        MoveType       == action.MoveType;

    protected override int GetCoreHashCode() => HashCode.Combine(Position, (int)MoveType);

    public override ExecuteActionBase DeepCopy() =>
        new MoveToPositionAction
        {
            Position  = Position,
            MoveType  = MoveType,
            Condition = ConditionCollection.Copy(Condition)
        };
}
