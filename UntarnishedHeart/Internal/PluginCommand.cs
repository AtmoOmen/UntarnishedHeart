using OmenTools.OmenService;
using UntarnishedHeart.Utils;
using UntarnishedHeart.Windows;

namespace UntarnishedHeart.Internal;

internal static class PluginCommand
{
    public const string MAIN_COMMAND = "/uth";

    public static void Init()
    {
        var manager = CommandManager.Instance();

        manager.MainCommand = new(MAIN_COMMAND, new(OnMainCommand) { HelpMessage = "打开主界面" });

        manager.AddSubCommand("autointeract", new(OnAutoInteract) { HelpMessage    = "尝试交互最近可交互物体" });
        manager.AddSubCommand("newround",     new(OnEnqueueNewRound) { HelpMessage = "若当前正在运行某一预设, 则立刻退出副本并开始新一轮执行" });
    }

    public static void Uninit()
    {
        var manager = CommandManager.Instance();

        manager.RemoveSubCommand("autointeract");
        manager.RemoveSubCommand("newround");
        manager.MainCommand = null;
    }

    private static void OnMainCommand(string command, string arguments)
    {
        if (WindowManager.Instance().Get<Main>() is { } main)
            main.IsOpen ^= true;
    }

    private static void OnAutoInteract(string command, string args) =>
        AutoObjectInteract.TryInteractNearestObject();

    private static void OnEnqueueNewRound(string command, string args) =>
        Main.PresetExecutor?.ManualEnqueueNewRound();
}
