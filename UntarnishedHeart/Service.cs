using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Plugin;
using OmenTools.OmenService;
using UntarnishedHeart.Internal;
using UntarnishedHeart.Utils;

namespace UntarnishedHeart;

public class Service
{
    public static PluginConfig Config { get; private set; } = null!;

    public static void Init(IDalamudPluginInterface pluginInterface)
    {
        DService.Init(pluginInterface);
        DService.Instance().UIBuilder.DisableCutsceneUiHide = true;

        Config = pluginInterface.GetPluginConfig() as PluginConfig ?? new PluginConfig();
        Config.Init();

        var notifyHelper = NotifyHelper.Instance();
        notifyHelper.ChatPrefix = new SeStringBuilder()
                                  .AddUiForeground(SeIconChar.BoxedLetterU.ToIconString(), 31)
                                  .AddUiForeground(SeIconChar.BoxedLetterT.ToIconString(), 31)
                                  .AddUiForeground(SeIconChar.BoxedLetterH.ToIconString(), 31)
                                  .Build();

        GameFunctions.Init();

        PluginWindow.Init();
        PluginCommand.Init();
    }

    public static void Uninit()
    {
        PluginCommand.Uninit();
        PluginWindow.Uninit();

        PluginConfig.Instance().Save();

        GameFunctions.Uninit();

        DService.Uninit();
    }
}
