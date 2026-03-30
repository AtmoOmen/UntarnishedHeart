using System.Reflection;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;

namespace UntarnishedHeart;

public sealed class Plugin : IDalamudPlugin
{
    public Plugin(IDalamudPluginInterface pi, IFramework framework)
    {
        if (Instance != null || IsDisposed || IsInitialized) return;
        IsInitialized = true;

        Version  ??= Assembly.GetExecutingAssembly().GetName().Version;
        Instance ??= this;

        Framework = framework;
        Framework.RunOnFrameworkThread(() => Service.Init(pi));
    }

    public const string PLUGIN_NAME = "Untarnished Heart";

    public static Version?        Version  { get; private set; }
    public static IDalamudPlugin? Instance { get; private set; }

    private bool IsDisposed    { get; set; }
    private bool IsInitialized { get; set; }

    private IFramework Framework { get; init; }

    public void Dispose()
    {
        if (Instance == null || IsDisposed || !IsInitialized) return;
        IsDisposed = true;

        Framework.RunOnFrameworkThread(Service.Uninit);
    }
}
