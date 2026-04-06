using Dalamud.Configuration;
using OmenTools.Interop.Game.Helpers;
using UntarnishedHeart.Execution.Enums;
using UntarnishedHeart.Execution.Preset;
using UntarnishedHeart.Execution.Route;
using UntarnishedHeart.Internal.Configuration;

namespace UntarnishedHeart.Internal;

[Serializable]
public class PluginConfig : IPluginConfiguration
{
    public int Version { get; set; } = PluginConfigMigrator.LatestVersion;

    public bool                 LeaderMode           { get; set; }
    public bool                 AutoRecommendGear    { get; set; }
    public int                  RunTimes             { get; set; } = -1;
    public List<Preset>         Presets              { get; set; } = [];
    public ContentsFinderOption ContentsFinderOption { get; set; } = ContentsFinderHelper.DefaultOption;
    public ContentEntryType     ContentEntryType     { get; set; } = ContentEntryType.Normal;

    // 运行路线相关配置
    public List<Route>   Routes               { get; set; } = [];
    public ExecutionMode CurrentExecutionMode { get; set; } = ExecutionMode.Preset;
    public int           SelectedRouteIndex   { get; set; } = -1;

    private static PluginConfig? InstanceInternal;

    public static PluginConfig Instance()
    {
        if (InstanceInternal != null) return InstanceInternal;

        Reload();
        PluginConfigMigrator.Migrate(InstanceInternal);
        return InstanceInternal;
    }

    internal static void Reload()
    {
        InstanceInternal = DService.Instance().PI.GetPluginConfig() as PluginConfig ??
                           new()
                           {
                               Presets =
                               [
                                   Preset.ExamplePreset0,
                                   Preset.ExamplePreset1,
                                   Preset.ExamplePreset2
                               ]
                           };
        InstanceInternal.Save();
    }

    internal void Save() =>
        DService.Instance().PI.SavePluginConfig(this);

    internal PresetExecutorRunOptions CreatePresetRunOptions() =>
        new(RunTimes, LeaderMode, AutoRecommendGear, ContentEntryType, ContentsFinderOption);
}
