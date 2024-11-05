using System;
using System.Collections.Generic;
using Dalamud.Configuration;
using UntarnishedHeart.Executor;

namespace UntarnishedHeart;

[Serializable]
public class Configuration : IPluginConfiguration
{
    public int                  Version          { get; set; } = 0;
    public MoveType             MoveType         { get; set; } = MoveType.传送;
    public bool                 LeaderMode       { get; set; } = false;
    public bool                 AutoOpenTreasure { get; set; } = false;
    public uint                 LeaveDutyDelay   { get; set; } = 0;
    public int                  RunTimes         { get; set; } = -1;
    public List<ExecutorPreset> Presets          { get; set; } = [];


    public static readonly ExecutorPreset ExamplePreset0 = new()
    {
        Name = "O5 魔列车", Zone = 748, Steps = [new() { DataID = 8510, Note = "魔列车", Position = new(0, 0, -15) }]
    };

    public static readonly ExecutorPreset ExamplePreset1 = new()
    {
        Name = "假火 (测试用)", Zone = 1045, Steps = [new() { DataID = 207, Note = "伊弗利特", Position = new(11, 0, 0) }]
    };

    public void Init()
    {
        if (Presets.Count == 0)
        {
            Presets.Add(ExamplePreset0);
            Presets.Add(ExamplePreset1);
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
