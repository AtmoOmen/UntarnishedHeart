using Dalamud.Plugin;

namespace SamplePlugin.Managers;

public class Service
{
    public static void Init(IDalamudPluginInterface pluginInterface)
    {
        DService.Init(pluginInterface);

        Config = pluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
        Config.Init();

        WindowManager.Init();
        CommandManager.Init();
    }

    public static void Uninit()
    {
        CommandManager.Uninit();
        WindowManager.Uninit();
        Config.Uninit();

        DService.Uninit();
    }

    public static Configuration  Config         { get; private set; } = null!;
    public static WindowManager  WindowManager  { get; private set; } = new();
    public static CommandManager CommandManager { get; private set; } = new();
}
