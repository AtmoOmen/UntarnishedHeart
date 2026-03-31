using UntarnishedHeart.Execution.Enums;

namespace UntarnishedHeart.Execution.Preset;

internal sealed class PresetExecutorResult
{
    public required ExecutorEndReason EndReason { get; init; }

    public required uint CompletedRounds { get; init; }

    public string ErrorMessage { get; init; } = string.Empty;
}
