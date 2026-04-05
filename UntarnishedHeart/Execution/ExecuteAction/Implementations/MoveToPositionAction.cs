using System.Numerics;
using UntarnishedHeart.Execution.Condition;
using UntarnishedHeart.Execution.Enums;
using UntarnishedHeart.Execution.ExecuteAction.Enums;

namespace UntarnishedHeart.Execution.ExecuteAction.Implementations;

public sealed class MoveToPositionAction : ExecuteAction
{
    public Vector3 Position { get; set; }

    public MoveType MoveType { get; set; } = MoveType.传送;

    public bool WaitForArrival { get; set; }

    public override ExecuteActionKind Kind => ExecuteActionKind.MoveToPosition;

    protected override bool EqualsCore(ExecuteAction other) =>
        other is MoveToPositionAction action &&
        Position.Equals(action.Position)     &&
        MoveType       == action.MoveType    &&
        WaitForArrival == action.WaitForArrival;

    protected override int GetCoreHashCode() => HashCode.Combine(Position, (int)MoveType, WaitForArrival);

    public override ExecuteAction DeepCopy() =>
        new MoveToPositionAction
        {
            Position       = Position,
            MoveType       = MoveType,
            WaitForArrival = WaitForArrival,
            Condition      = ConditionCollection.Copy(Condition)
        };
}
