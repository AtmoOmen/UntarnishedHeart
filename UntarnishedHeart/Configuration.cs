using System;
using System.Collections.Generic;
using Dalamud.Configuration;
using UntarnishedHeart.Executor;

namespace UntarnishedHeart;

[Serializable]
public class Configuration : IPluginConfiguration
{
    public int                  Version              { get; set; } = 0;
    public bool                 LeaderMode           { get; set; }
    public bool                 AutoRecommendGear    { get; set; }
    public int                  RunTimes             { get; set; } = -1;
    public List<ExecutorPreset> Presets              { get; set; } = [];
    public ContentsFinderOption ContentsFinderOption { get; set; } = ContentsFinderHelper.DefaultOption;
    public ContentEntryType     ContentEntryType     { get; set; } = ContentEntryType.Normal;
    
    // 运行路线相关配置
    public List<Route>          Routes               { get; set; } = [];
    public ExecutionMode        CurrentExecutionMode { get; set; } = ExecutionMode.Simple;
    public int                  SelectedRouteIndex   { get; set; } = -1;


    public static readonly ExecutorPreset ExamplePreset0 = new()
    {
        Name = "O5 魔列车", Zone = 748, Steps = [new() { DataID = 8510, Note = "魔列车", Position = new(0, 0, -15) }]
    };

    public static readonly ExecutorPreset ExamplePreset1 = new()
    {
        Name = "假火 (测试用)", Zone = 1045, Steps = [new() { DataID = 207, Note = "伊弗利特", Position = new(11, 0, 0) }]
    };

    public static readonly ExecutorPreset ExamplePreset2 = new()
    {
        Name = "极风 (测试用)", Zone = 297, Steps =
        [
            new() { DataID = 245, Note = "Note1", Position = new(-0.24348414f, -1.9395045f, -14.213441f), Delay = 8000, StopInCombat = false },
            new() { DataID = 245, Note = "Note2", Position = new(-0.63603175f, -1.8021163f, 0.6449276f), Delay  = 5000, StopInCombat = false }
        ]
    };

    public void Init()
    {
        if (Presets.Count == 0)
        {
            Presets.Add(ExamplePreset0);
            Presets.Add(ExamplePreset1);
            Presets.Add(ExamplePreset2);
            Save();
        }
    }

    public void Save()
    {
        DService.PI.SavePluginConfig(this);
    }

    public void Uninit()
    {
        Save();
    }
}
