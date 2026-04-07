using UntarnishedHeart.Execution.ExecuteAction.Enums;
using UntarnishedHeart.Execution.ExecuteAction.Helpers;
using UntarnishedHeart.Execution.Preset.Helpers;

namespace UntarnishedHeart.Execution.ExecuteAction.Implementations;

public sealed class InteractTargetAction : ExecuteActionBase
{
    public TargetSelector Selector { get; set; } = new();

    public bool OpenObjectInteraction { get; set; }

    public override ExecuteActionKind Kind => ExecuteActionKind.InteractTarget;

    public override void Draw()
    {
        ExecuteActionDrawHelper.DrawTargetSelector(Selector, "InteractTarget");

        var openObjectInteraction = OpenObjectInteraction;
        if (ImGui.Checkbox("打开对象交互###OpenObjectInteraction", ref openObjectInteraction))
            OpenObjectInteraction = openObjectInteraction;
        ImGuiOm.HelpMarker("部分 Object Kind 对象需要启用本项才能正常交互\n如: EventObj、EventNpc 等");
    }

    protected override bool EqualsCore(ExecuteActionBase other) =>
        other is InteractTargetAction action &&
        Selector.Equals(action.Selector)     &&
        OpenObjectInteraction == action.OpenObjectInteraction;

    protected override int GetCoreHashCode() =>
        HashCode.Combine(Selector, OpenObjectInteraction);

    public override ExecuteActionBase DeepCopy() =>
        CopyBasePropertiesTo
        (
            new InteractTargetAction
            {
                Selector              = TargetSelector.Copy(Selector),
                OpenObjectInteraction = OpenObjectInteraction
            }
        );
}
