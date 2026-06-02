using Dalamud.Game.Text;
using Dalamud.Plugin;
using Dalamud.Utility;
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

        var       notifyHelper = NotifyHelper.Instance();
        using var rented       = new RentedSeStringBuilder();

        notifyHelper.ChatPrefix = rented.Builder
                                        .PushColorType(31)
                                        .Append(SeIconChar.BoxedLetterU.ToIconString())
                                        .Append(SeIconChar.BoxedLetterT.ToIconString())
                                        .Append(SeIconChar.BoxedLetterH.ToIconString())
                                        .PopColorType()
                                        .ToReadOnlySeString();
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
