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

    public bool WaitForArrival { get; set; }

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

        var waitForArrival = WaitForArrival;
        if (ImGui.Checkbox("等待接近后再继续###WaitForArrivalInput", ref waitForArrival))
            WaitForArrival = waitForArrival;
        ImGuiOm.HelpMarker("不推荐使用这一选项, 目前仅做兼容性功能提供, 后续可能会删除\n建议使用条件组, 设置处理类型为\"持续\", 新增条件 \"坐标范围\" 来实现更加精细的判断");
    }

    protected override bool EqualsCore(ExecuteActionBase other) =>
        other is MoveToPositionAction action &&
        Position.Equals(action.Position)     &&
        MoveType       == action.MoveType    &&
        WaitForArrival == action.WaitForArrival;

    protected override int GetCoreHashCode() => HashCode.Combine(Position, (int)MoveType, WaitForArrival);

    public override ExecuteActionBase DeepCopy() =>
        new MoveToPositionAction
        {
            Position       = Position,
            MoveType       = MoveType,
            WaitForArrival = WaitForArrival,
            Condition      = ConditionCollection.Copy(Condition)
        };
}
