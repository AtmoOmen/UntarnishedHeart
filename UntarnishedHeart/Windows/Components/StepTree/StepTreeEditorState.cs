namespace UntarnishedHeart.Windows.Components;

internal sealed class StepTreeEditorState
{
    public int CurrentStep { get; set; } = -1;

    public int CurrentAction { get; set; } = -1;

    public Dictionary<int, int> StepActionSelections { get; } = [];

    public StepTreeNodeKind CurrentNodeKind { get; set; } = StepTreeNodeKind.Step;

    public string FilterText { get; set; } = string.Empty;

    public int PendingOpenStep { get; set; } = -1;

    public string CurrentPathTabLabel { get; set; } = string.Empty;
}
