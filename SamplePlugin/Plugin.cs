global using static OmenTools.Helpers.HelpersOm;
global using static OmenTools.Infos.InfosOm;
global using static OmenTools.Helpers.ThrottlerHelper;
global using OmenTools.Infos;
global using OmenTools.ImGuiOm;
global using OmenTools.Helpers;
global using OmenTools;
global using static SamplePlugin.Plugin;
using System;
using System.Reflection;
using Dalamud.Plugin;
using SamplePlugin.Managers;

namespace SamplePlugin;

public sealed class Plugin : IDalamudPlugin
{
    public static string PluginName => "Omen Sample Plugin";
    public static Version? Version { get; private set; }

    public Plugin(IDalamudPluginInterface pluginInterface)
    {
        Version ??= Assembly.GetExecutingAssembly().GetName().Version;

        Service.Init(pluginInterface);
    }

    public void Dispose()
    {
        Service.Uninit();
    }
}
