using UntarnishedHeart.Execution.Common;

namespace UntarnishedHeart.Execution.Preset;

internal sealed class PresetExecutorProgress
{
    public required uint CurrentRound { get; init; }

    public required int MaxRound { get; init; }

    public required string RunningMessage { get; init; }

    public required bool IsFinished { get; init; }

    public required bool IsStopped { get; init; }

    public required ExecuteActionRuntimeCursor RuntimeCursor { get; init; }
}
