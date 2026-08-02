using UntarnishedHeart.Execution.Preset;

namespace UntarnishedHeart.Execution.ExecuteAction;

public sealed class ExecuteActionDrawContext
{
    public required IList<PresetStep> Steps { get; init; }

    public required IList<ExecuteActionBase> Actions { get; init; }
}
