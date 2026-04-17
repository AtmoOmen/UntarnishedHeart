using UntarnishedHeart.Execution.Common;
using OmenTools.Interop.Game.Helpers;
using UntarnishedHeart.Execution.Enums;

namespace UntarnishedHeart.Execution.Preset;

internal sealed class PresetExecutorRunOptions
{
    public PresetExecutorRunOptions
    (
        int                       maxRound,
        bool                      leaderMode,
        bool                      autoRecommendGear,
        ContentEntryType          contentEntryType,
        ContentsFinderOption      contentsFinderOption,
        ExecuteActionRuntimeCursor? startCursor = null
    )
    {
        MaxRound             = maxRound;
        LeaderMode           = leaderMode;
        AutoRecommendGear    = autoRecommendGear;
        ContentEntryType     = contentEntryType;
        ContentsFinderOption = contentsFinderOption.Clone();
        StartCursor          = startCursor == null ? null : new(startCursor.StepIndex, startCursor.Phase, startCursor.ActionIndex);
    }

    public int MaxRound { get; }

    public bool LeaderMode { get; }

    public bool AutoRecommendGear { get; }

    public ContentEntryType ContentEntryType { get; }

    public ContentsFinderOption ContentsFinderOption { get; }

    public ExecuteActionRuntimeCursor? StartCursor { get; }
}
