global using static OmenTools.Helpers.HelpersOm;
global using static OmenTools.Infos.InfosOm;
global using static OmenTools.Helpers.ThrottlerHelper;
global using OmenTools.Infos;
global using OmenTools.ImGuiOm;
global using OmenTools.Helpers;
global using OmenTools;
global using static UntarnishedHeart.Plugin;
global using static UntarnishedHeart.Utils.Widgets;
using System;
using System.Reflection;
using Dalamud.Plugin;
using UntarnishedHeart.Managers;

namespace UntarnishedHeart;

public sealed class Plugin : IDalamudPlugin
{
    public const string PluginName = "Untarnished Heart";

    public Plugin(IDalamudPluginInterface pluginInterface)
    {
        Version ??= Assembly.GetExecutingAssembly().GetName().Version;

        Service.Init(pluginInterface);
    }

    public static Version? Version { get; private set; }

    public void Dispose() =>
        Service.Uninit();
}
