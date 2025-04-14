using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using Dalamud.Game.Command;
using UntarnishedHeart.Utils;
using UntarnishedHeart.Windows;

namespace UntarnishedHeart.Managers;

public sealed class CommandManager
{
    public const string MainCommand = "/uth";

    private static readonly ConcurrentDictionary<string, CommandInfo> addedCommands = [];
    private static readonly ConcurrentDictionary<string, CommandInfo> subCommands   = [];
    
    internal void Init()
    {
        RefreshCommandDetails();
        InternalCommands.Init();
    }

    private static void RefreshCommandDetails()
    {
        var helpMessage = new StringBuilder("打开主界面\n");
        
        foreach (var (command, commandInfo) in subCommands.Where(x => x.Value.ShowInHelp))
            helpMessage.AppendLine($"{MainCommand} {command} → {commandInfo.HelpMessage}");

        RemoveCommand(MainCommand);
        AddCommand(MainCommand, new CommandInfo(OnCommandPDR) { HelpMessage = helpMessage.ToString() }, true);
    }

    public static bool AddCommand(string command, CommandInfo commandInfo, bool isForceToAdd = false)
    {
        if (!isForceToAdd && DService.Command.Commands.ContainsKey(command)) return false;

        RemoveCommand(command);
        DService.Command.AddHandler(command, commandInfo);
        addedCommands[command] = commandInfo;

        return true;
    }

    public static bool RemoveCommand(string command)
    {
        if (DService.Command.Commands.ContainsKey(command))
        {
            DService.Command.RemoveHandler(command);
            addedCommands.TryRemove(command, out _);
            return true;
        }

        return false;
    }

    public static bool AddSubCommand(string args, CommandInfo commandInfo, bool isForceToAdd = false)
    {
        if (!isForceToAdd && subCommands.ContainsKey(args)) return false;

        subCommands[args] = commandInfo;
        RefreshCommandDetails();
        return true;
    }

    public static bool RemoveSubCommand(string args)
    {
        if (subCommands.TryRemove(args, out _))
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
        if (subCommands.TryGetValue(spitedArgs[0], out var commandInfo))
            commandInfo.Handler(spitedArgs[0], spitedArgs.Length > 1 ? spitedArgs[1] : string.Empty);
        else
            ChatError($"子命令 {spitedArgs[0]} 不存在");
    }

    internal void Uninit()
    {
        foreach (var command in addedCommands.Keys)
            RemoveCommand(command);

        addedCommands.Clear();
        subCommands.Clear();
    }

    private static class InternalCommands
    {
        public const string AutoInteractCommand    = "autointeract";
        public const string EnqueueNewRoundCommand = "newround";
        
        internal static void Init()
        {
            AddSubCommand(AutoInteractCommand,    new(OnCommandAutoInteract) { HelpMessage    = "尝试交互最近可交互物体" });
            AddSubCommand(EnqueueNewRoundCommand, new(OnCommandEnqueueNewRound) { HelpMessage = "若当前正在运行某一预设, 则立刻退出副本并开始新一轮执行" });
        }

        private static void OnCommandAutoInteract(string command, string args) => 
            AutoObjectInteract.TryInteractNearestObject();
        
        private static void OnCommandEnqueueNewRound(string command, string args) => 
            Main.PresetExecutor?.ManualEnqueueNewRound();
    }
}
