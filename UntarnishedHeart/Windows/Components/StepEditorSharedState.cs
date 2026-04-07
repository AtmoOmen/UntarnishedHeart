using UntarnishedHeart.Execution.ExecuteAction;
using UntarnishedHeart.Execution.Preset;

namespace UntarnishedHeart.Windows.Components;

internal sealed class StepEditorSharedState
{
    public PresetStep?        StepToCopy   { get; set; }
    public ExecuteActionBase? ActionToCopy { get; set; }
}
