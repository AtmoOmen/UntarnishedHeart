using UntarnishedHeart.Execution.Condition;
using UntarnishedHeart.Execution.ExecuteAction.Enums;

namespace UntarnishedHeart.Execution.ExecuteAction.Implementations;

public sealed class WaitMillisecondsAction : ExecuteActionBase
{
    public int Milliseconds { get; set; }

    public override ExecuteActionKind Kind => ExecuteActionKind.WaitMilliseconds;

    public override void Draw()
    {
        var milliseconds = Milliseconds;
        ImGui.SetNextItemWidth(240f * GlobalUIScale);
        if (ImGui.InputInt("等待毫秒###WaitMillisecondsInput", ref milliseconds))
            Milliseconds = Math.Max(0, milliseconds);
    }

    protected override bool EqualsCore(ExecuteActionBase other) => other is WaitMillisecondsAction action && Milliseconds == action.Milliseconds;

    protected override int GetCoreHashCode() => Milliseconds;

    public override ExecuteActionBase DeepCopy() =>
        new WaitMillisecondsAction
        {
            Milliseconds = Milliseconds,
            Condition    = ConditionCollection.Copy(Condition)
        };
}
