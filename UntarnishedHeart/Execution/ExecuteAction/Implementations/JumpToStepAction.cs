using UntarnishedHeart.Execution.Condition;
using UntarnishedHeart.Execution.ExecuteAction.Enums;

namespace UntarnishedHeart.Execution.ExecuteAction.Implementations;

public sealed class JumpToStepAction : ExecuteActionBase
{
    public int StepIndex { get; set; }

    public override ExecuteActionKind Kind => ExecuteActionKind.JumpToStep;

    public override void Draw()
    {
        var stepIndex = StepIndex;
        ImGui.SetNextItemWidth(240f * GlobalUIScale);
        if (ImGui.InputInt("目标步骤索引###JumpToStepInput", ref stepIndex))
            StepIndex = stepIndex;
    }

    protected override bool EqualsCore(ExecuteActionBase other) => other is JumpToStepAction action && StepIndex == action.StepIndex;

    protected override int GetCoreHashCode() => StepIndex;

    public override ExecuteActionBase DeepCopy() =>
        CopyBasePropertiesTo(new JumpToStepAction { StepIndex = StepIndex });
}
