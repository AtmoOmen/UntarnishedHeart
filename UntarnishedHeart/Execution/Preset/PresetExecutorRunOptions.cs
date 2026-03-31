using OmenTools.Interop.Game.Helpers;
using UntarnishedHeart.Execution.Enums;

namespace UntarnishedHeart.Execution.Preset;

internal sealed class PresetExecutorRunOptions
{
    public PresetExecutorRunOptions
        (int maxRound, bool leaderMode, bool autoRecommendGear, ContentEntryType contentEntryType, ContentsFinderOption contentsFinderOption)
    {
        MaxRound             = maxRound;
        LeaderMode           = leaderMode;
        AutoRecommendGear    = autoRecommendGear;
        ContentEntryType     = contentEntryType;
        ContentsFinderOption = contentsFinderOption.Clone();
    }

    public int MaxRound { get; }

    public bool LeaderMode { get; }

    public bool AutoRecommendGear { get; }

    public ContentEntryType ContentEntryType { get; }

    public ContentsFinderOption ContentsFinderOption { get; }
}
