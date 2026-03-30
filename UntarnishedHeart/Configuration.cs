using Dalamud.Configuration;
using OmenTools.Interop.Game.Helpers;
using UntarnishedHeart.Executor;

namespace UntarnishedHeart;

[Serializable]
public class Configuration : IPluginConfiguration
{
    public bool                 LeaderMode           { get; set; }
    public bool                 AutoRecommendGear    { get; set; }
    public int                  RunTimes             { get; set; } = -1;
    public List<ExecutorPreset> Presets              { get; set; } = [];
    public ContentsFinderOption ContentsFinderOption { get; set; } = ContentsFinderHelper.DefaultOption;
    public ContentEntryType     ContentEntryType     { get; set; } = ContentEntryType.Normal;

    // 运行路线相关配置
    public List<Route>   Routes               { get; set; } = [];
    public ExecutionMode CurrentExecutionMode { get; set; } = ExecutionMode.Simple;
    public int           SelectedRouteIndex   { get; set; } = -1;
    public int           Version              { get; set; } = 0;

    public void Init()
    {
        if (Presets.Count == 0)
        {
            Presets.Add(ExecutorPreset.ExamplePreset0);
            Presets.Add(ExecutorPreset.ExamplePreset1);
            Presets.Add(ExecutorPreset.ExamplePreset2);
            Save();
        }
    }

    public void Save() =>
        DService.Instance().PI.SavePluginConfig(this);

    public void Uninit() =>
        Save();
}
