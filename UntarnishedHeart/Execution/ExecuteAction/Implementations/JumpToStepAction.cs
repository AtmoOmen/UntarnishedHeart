using Newtonsoft.Json;
using UntarnishedHeart.Execution.ExecuteAction.Configuration;
using UntarnishedHeart.Execution.ExecuteAction.Enums;
using UntarnishedHeart.Windows;

namespace UntarnishedHeart.Execution.ExecuteAction.Implementations;

[JsonObject(MemberSerialization.OptIn)]
[ExecuteActionJsonType("JumpToStep", ExecuteActionKind.JumpToStep)]
public sealed class JumpToStepAction : ExecuteActionBase
{
    [JsonProperty("StepIndex")]
    public int StepIndex { get; set; }

    public override ExecuteActionKind Kind => ExecuteActionKind.JumpToStep;

    public override void Draw
    (
        ExecuteActionDrawContext context
    )
    {
        var hasValidTarget = StepIndex >= 0 && StepIndex < context.Steps.Count;
        var preview        = StepIndex < 0 ? "当前步骤" : hasValidTarget ? $"{StepIndex}. {context.Steps[StepIndex].Name}" : "请选择目标步骤";
        ImGui.SetNextItemWidth(240f * GlobalUIScale);

        using var combo = ImRaii.Combo("目标步骤###JumpToStepCombo", preview, ImGuiComboFlags.HeightLargest);
        if (combo)
            ImGui.CloseCurrentPopup();

        if (!ImGui.IsItemClicked())
            return;

        CollectionSelectorWindow.Open
        (
            "选择目标步骤",
            "暂无步骤",
            hasValidTarget ?
                StepIndex :
                -1,
            context.Steps,
            static step => step.Name,
            index =>
            {
                if ((uint)index >= (uint)context.Steps.Count)
                    return;

                StepIndex = index;
            }
        );
    }

    protected override bool EqualsCore
    (
        ExecuteActionBase other
    ) =>
        other is JumpToStepAction action && StepIndex == action.StepIndex;

    protected override int GetCoreHashCode() =>
        StepIndex;

    public override ExecuteActionBase DeepCopy() =>
        CopyBasePropertiesTo(new JumpToStepAction { StepIndex = StepIndex });
}
