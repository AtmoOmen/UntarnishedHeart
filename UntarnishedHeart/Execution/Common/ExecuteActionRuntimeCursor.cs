namespace UntarnishedHeart.Execution.Common;

public sealed class ExecuteActionRuntimeCursor
(
    int stepIndex,
    int actionIndex
)
{
    public static ExecuteActionRuntimeCursor Empty { get; } = new(-1, -1);

    public int StepIndex { get; } = stepIndex;

    public int ActionIndex { get; } = actionIndex;

    public bool HasStep => StepIndex >= 0;

    public bool HasAction => HasStep && ActionIndex >= 0;
}
