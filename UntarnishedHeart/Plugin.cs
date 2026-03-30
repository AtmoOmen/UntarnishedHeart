using System.Reflection;
using Dalamud.Plugin;
using UntarnishedHeart.Managers;

namespace UntarnishedHeart;

public sealed class Plugin : IDalamudPlugin
{
    public const string PLUGIN_NAME = "Untarnished Heart";

    public Plugin(IDalamudPluginInterface pluginInterface)
    {
        Version ??= Assembly.GetExecutingAssembly().GetName().Version;

        Service.Init(pluginInterface);
    }

    public static Version? Version { get; private set; }

    public void Dispose() =>
        Service.Uninit();
}
