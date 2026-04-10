using UntarnishedHeart.Execution.Preset.Enums;

namespace UntarnishedHeart.Execution.Common;

public sealed class ExecuteActionRuntimeCursor
(
    int              stepIndex,
    PresetStepPhase? phase,
    int              actionIndex
)
{
    public static ExecuteActionRuntimeCursor Empty { get; } = new(-1, null, -1);

    public int StepIndex { get; } = stepIndex;

    public PresetStepPhase? Phase { get; } = phase;

    public int ActionIndex { get; } = actionIndex;

    public bool HasStep => StepIndex >= 0;

    public bool HasPhase => HasStep && Phase.HasValue;

    public bool HasAction => HasPhase && ActionIndex >= 0;
}
