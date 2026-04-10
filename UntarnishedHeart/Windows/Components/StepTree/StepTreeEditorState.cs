using UntarnishedHeart.Execution.Preset.Enums;

namespace UntarnishedHeart.Windows.Components;

internal sealed class StepTreeEditorState
{
    public int CurrentStep { get; set; } = -1;

    public PresetStepPhase CurrentPhase { get; set; } = PresetStepPhase.Enter;

    public int CurrentAction { get; set; } = -1;

    public StepTreeNodeKind CurrentNodeKind { get; set; } = StepTreeNodeKind.Step;

    public string FilterText { get; set; } = string.Empty;

    public int PendingOpenStep { get; set; } = -1;

    public PresetStepPhase? PendingOpenPhase { get; set; }

    public string CurrentPathTabLabel { get; set; } = "当前路径";
}
