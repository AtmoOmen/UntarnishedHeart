using Newtonsoft.Json;
using OmenTools.Interop.Game.Helpers;
using UntarnishedHeart.Execution.Enums;
using UntarnishedHeart.Execution.Preset;

namespace UntarnishedHeart.Execution.Route;

[JsonObject(MemberSerialization.OptIn)]
public sealed class DutyOptions : IEquatable<DutyOptions>
{
    [JsonProperty("LeaderMode")]
    public bool LeaderMode { get; set; }

    [JsonProperty("AutoRecommendGear")]
    public bool AutoRecommendGear { get; set; }

    [JsonProperty("RunTimes")]
    public int RunTimes { get; set; } = 1;

    [JsonProperty("ContentEntryType")]
    public ContentEntryType ContentEntryType { get; set; } = ContentEntryType.Normal;

    [JsonProperty("ContentsFinderOption")]
    public ContentsFinderOption ContentsFinderOption { get; set; } = ContentsFinderHelper.DefaultOption.Clone();

    internal PresetExecutorRunOptions ToRunOptions() =>
        new(RunTimes, LeaderMode, AutoRecommendGear, ContentEntryType, ContentsFinderOption);

    public bool Equals(DutyOptions? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;

        return LeaderMode        == other.LeaderMode        &&
               AutoRecommendGear == other.AutoRecommendGear &&
               RunTimes          == other.RunTimes          &&
               ContentEntryType  == other.ContentEntryType  &&
               ContentsFinderOption.Equals(other.ContentsFinderOption);
    }

    public override bool Equals(object? obj) => Equals(obj as DutyOptions);

    public override int GetHashCode() => HashCode.Combine(LeaderMode, AutoRecommendGear, RunTimes, ContentEntryType, ContentsFinderOption);

    public static DutyOptions Copy(DutyOptions source) =>
        new()
        {
            LeaderMode           = source.LeaderMode,
            AutoRecommendGear    = source.AutoRecommendGear,
            RunTimes             = source.RunTimes,
            ContentEntryType     = source.ContentEntryType,
            ContentsFinderOption = source.ContentsFinderOption.Clone()
        };
}
