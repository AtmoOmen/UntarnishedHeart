using UntarnishedHeart.Execution.Preset.Enums;

namespace UntarnishedHeart.Windows.Components;

internal sealed class StepTreeExecutionStartOptions
{
    public bool IsVisible { get; init; }

    public Action<int>? StartFromStep { get; init; }

    public Action<int, PresetStepPhase>? StartFromPhase { get; init; }

    public Action<int, PresetStepPhase, int>? StartFromAction { get; init; }
}
