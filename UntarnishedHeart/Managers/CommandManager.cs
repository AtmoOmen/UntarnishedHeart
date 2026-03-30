using System.Collections.Concurrent;
using System.Text;
using Dalamud.Game.Command;
using OmenTools.OmenService;
using UntarnishedHeart.Utils;
using UntarnishedHeart.Windows;

namespace UntarnishedHeart.Managers;

public sealed class CommandManager
{
    public const string MAIN_COMMAND = "/uth";

    private static readonly ConcurrentDictionary<string, CommandInfo> AddedCommands = [];
    private static readonly ConcurrentDictionary<string, CommandInfo> SubCommands   = [];

    internal void Init()
    {
        RefreshCommandDetails();
        InternalCommands.Init();
    }

    private static void RefreshCommandDetails()
    {
        var helpMessage = new StringBuilder("打开主界面\n");

        foreach (var (command, commandInfo) in SubCommands.Where(x => x.Value.ShowInHelp))
            helpMessage.AppendLine($"{MAIN_COMMAND} {command} → {commandInfo.HelpMessage}");

        RemoveCommand(MAIN_COMMAND);
        AddCommand(MAIN_COMMAND, new CommandInfo(OnCommandPDR) { HelpMessage = helpMessage.ToString() }, true);
    }

    public static bool AddCommand(string command, CommandInfo commandInfo, bool isForceToAdd = false)
    {
        if (!isForceToAdd && DService.Instance().Command.Commands.ContainsKey(command)) return false;

        RemoveCommand(command);
        DService.Instance().Command.AddHandler(command, commandInfo);
        AddedCommands[command] = commandInfo;

        return true;
    }

    public static bool RemoveCommand(string command)
    {
        if (DService.Instance().Command.Commands.ContainsKey(command))
        {
            DService.Instance().Command.RemoveHandler(command);
            AddedCommands.TryRemove(command, out _);
            return true;
        }

        return false;
    }

    public static bool AddSubCommand(string args, CommandInfo commandInfo, bool isForceToAdd = false)
    {
        if (!isForceToAdd && SubCommands.ContainsKey(args)) return false;

        SubCommands[args] = commandInfo;
        RefreshCommandDetails();
        return true;
    }

    public static bool RemoveSubCommand(string args)
    {
        if (SubCommands.TryRemove(args, out _))
        {
            RefreshCommandDetails();
            return true;
        }

        return false;
    }

    private static void OnCommandPDR(string command, string args)
    {
        if (string.IsNullOrWhiteSpace(args))
        {
            if (WindowManager.Get<Main>() is { } main)
                main.IsOpen ^= true;
            return;
        }

        var spitedArgs = args.Split(' ', 2);
        if (SubCommands.TryGetValue(spitedArgs[0], out var commandInfo))
            commandInfo.Handler(spitedArgs[0], spitedArgs.Length > 1 ? spitedArgs[1] : string.Empty);
        else
            NotifyHelper.Instance().ChatError($"子命令 {spitedArgs[0]} 不存在");
    }

    internal void Uninit()
    {
        foreach (var command in AddedCommands.Keys)
            RemoveCommand(command);

        AddedCommands.Clear();
        SubCommands.Clear();
    }

    private static class InternalCommands
    {
        public const string AUTO_INTERACT_COMMAND    = "autointeract";
        public const string ENQUEUE_NEW_ROUND_COMMAND = "newround";

        internal static void Init()
        {
            AddSubCommand(AUTO_INTERACT_COMMAND,    new(OnCommandAutoInteract) { HelpMessage    = "尝试交互最近可交互物体" });
            AddSubCommand(ENQUEUE_NEW_ROUND_COMMAND, new(OnCommandEnqueueNewRound) { HelpMessage = "若当前正在运行某一预设, 则立刻退出副本并开始新一轮执行" });
        }

        private static void OnCommandAutoInteract(string command, string args) =>
            AutoObjectInteract.TryInteractNearestObject();

        private static void OnCommandEnqueueNewRound(string command, string args) =>
            Main.PresetExecutor?.ManualEnqueueNewRound();
    }
}
