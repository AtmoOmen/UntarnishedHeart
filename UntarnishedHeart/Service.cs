using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Plugin;
using OmenTools.OmenService;
using UntarnishedHeart.Execution.Managers;
using UntarnishedHeart.Internal;

namespace UntarnishedHeart;

public class Service
{
    public static void Init(IDalamudPluginInterface pluginInterface)
    {
        DService.Init(pluginInterface);
        DService.Instance().UIBuilder.DisableCutsceneUiHide = true;

        var notifyHelper = NotifyHelper.Instance();
        notifyHelper.ChatPrefix = new SeStringBuilder()
                                  .AddUiForeground(SeIconChar.BoxedLetterU.ToIconString(), 31)
                                  .AddUiForeground(SeIconChar.BoxedLetterT.ToIconString(), 31)
                                  .AddUiForeground(SeIconChar.BoxedLetterH.ToIconString(), 31)
                                  .Build();
        PluginWindow.Init();
        PluginCommand.Init();
    }

    public static void Uninit()
    {
        PluginCommand.Uninit();
        ExecutionManager.Dispose();
        PluginWindow.Uninit();

        PluginConfig.Instance().Save();
        DService.Uninit();
    }
}
