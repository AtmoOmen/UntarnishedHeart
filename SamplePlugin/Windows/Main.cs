using System;
using Dalamud.Interface.Windowing;
using ImGuiNET;

namespace SamplePlugin.Windows;

public class Main() : Window($"{PluginName} 配置窗口", ImGuiWindowFlags.AlwaysAutoResize), IDisposable
{
    public override void Draw() { }

    public void Dispose() { }
}
