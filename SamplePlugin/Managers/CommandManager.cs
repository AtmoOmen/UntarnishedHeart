using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using Dalamud.Game.Command;

namespace SamplePlugin.Managers;

public sealed class CommandManager
{
    public const string CommandPDR = "/omspp";
    private static readonly ConcurrentDictionary<string, CommandInfo> _addedCommands = [];
    private static readonly ConcurrentDictionary<string, CommandInfo> _subCommandArgs = [];
    private static readonly object _lock = new();

    internal void Init()
    {
        RefreshCommandDetails();
    }

    private void RefreshCommandDetails()
    {
        lock (_lock)
        {
            var helpMessage = new StringBuilder("打开主界面");

            foreach (var (command, commandInfo) in _subCommandArgs.Where(x => x.Value.ShowInHelp))
                helpMessage.AppendLine($"{CommandPDR} {command} → {commandInfo.HelpMessage}");

            RemoveCommand(CommandPDR);
            AddCommand(CommandPDR, new CommandInfo(OnCommandMain) { HelpMessage = helpMessage.ToString() }, true);
        }
    }

    public bool AddCommand(string command, CommandInfo commandInfo, bool isForceToAdd = false)
    {
        lock (_lock)
        {
            if (!isForceToAdd && DService.Command.Commands.ContainsKey(command)) return false;

            RemoveCommand(command);
            DService.Command.AddHandler(command, commandInfo);
            _addedCommands[command] = commandInfo;

            return true;
        }
    }

    public bool RemoveCommand(string command)
    {
        lock (_lock)
        {
            if (DService.Command.Commands.ContainsKey(command))
            {
                DService.Command.RemoveHandler(command);
                _addedCommands.TryRemove(command, out _);
                return true;
            }

            return false;
        }
    }

    public bool AddSubCommand(string args, CommandInfo commandInfo, bool isForceToAdd = false)
    {
        lock (_lock)
        {
            if (!isForceToAdd && _subCommandArgs.ContainsKey(args)) return false;

            _subCommandArgs[args] = commandInfo;
            RefreshCommandDetails();
            return true;
        }
    }

    public bool RemoveSubCommand(string args)
    {
        lock (_lock)
        {
            if (_subCommandArgs.TryRemove(args, out _))
            {
                RefreshCommandDetails();
                return true;
            }

            return false;
        }
    }

    private static void OnCommandMain(string command, string args)
    {
        WindowManager.Main.IsOpen ^= true;

        var spitedArgs = args.Split(' ', 2);
        if (_subCommandArgs.TryGetValue(spitedArgs[0], out var commandInfo))
            commandInfo.Handler(spitedArgs[0], spitedArgs.Length > 1 ? spitedArgs[1] : string.Empty);
        else
            DService.Chat.PrintError($"{spitedArgs[0]} 出现问题：该命令不存在。");
    }

    internal void Uninit()
    {
        foreach (var command in _addedCommands.Keys)
            RemoveCommand(command);

        _addedCommands.Clear();
        _subCommandArgs.Clear();
    }
}
