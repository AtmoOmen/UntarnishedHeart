using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Plugin;
using OmenTools.OmenService;
using UntarnishedHeart.Utils;

namespace UntarnishedHeart.Managers;

public class Service
{
    public static Configuration  Config         { get; private set; } = null!;
    public static WindowManager  WindowManager  { get; private set; } = new();
    public static CommandManager CommandManager { get; private set; } = new();

    public static void Init(IDalamudPluginInterface pluginInterface)
    {
        DService.Init(pluginInterface);
        DService.Instance().UIBuilder.DisableCutsceneUiHide = true;

        Config = pluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
        Config.Init();

        var notifyHelper = NotifyHelper.Instance();
        notifyHelper.ChatPrefix = new SeStringBuilder()
                                  .AddUiForeground(SeIconChar.BoxedLetterU.ToIconString(), 31)
                                  .AddUiForeground(SeIconChar.BoxedLetterT.ToIconString(), 31)
                                  .AddUiForeground(SeIconChar.BoxedLetterH.ToIconString(), 31)
                                  .Build();

        GameFunctions.Init();

        WindowManager.Init();
        CommandManager.Init();
    }

    public static void Uninit()
    {
        CommandManager.Uninit();
        WindowManager.Uninit();
        Config.Uninit();

        GameFunctions.Uninit();

        DService.Uninit();
    }
}
