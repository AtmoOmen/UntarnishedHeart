using Dalamud.Plugin;
using UntarnishedHeart.Utils;

namespace UntarnishedHeart.Managers;

public class Service
{
    public static void Init(IDalamudPluginInterface pluginInterface)
    {
        DService.Init(pluginInterface);
        DService.UIBuilder.DisableCutsceneUiHide = true;

        Config = pluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
        Config.Init();

        GameFunctions.Init();

        FontManager.Init();
        
        WindowManager.Init();
        CommandManager.Init();
    }

    public static void Uninit()
    {
        CommandManager.Uninit();
        WindowManager.Uninit();
        Config.Uninit();
        
        FontManager.Uninit();

        GameFunctions.Uninit();

        DService.Uninit();
    }

    public static Configuration  Config         { get; private set; } = null!;
    public static WindowManager  WindowManager  { get; private set; } = new();
    public static CommandManager CommandManager { get; private set; } = new();
}
