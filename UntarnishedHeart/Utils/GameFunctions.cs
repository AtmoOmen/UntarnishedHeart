using System.Numerics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Memory;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;

namespace UntarnishedHeart.Utils;

public static class GameFunctions
{
    private static readonly CompSig ExecuteCommandSig = 
        new("E8 ?? ?? ?? ?? 48 8B 5C 24 ?? 48 8B 74 24 ?? 48 83 C4 ?? 5F C3 CC CC CC CC CC CC CC CC CC CC 48 89 5C 24 ?? 57 48 83 EC ?? 80 A1");
    private delegate nint ExecuteCommandDelegate(int command, int param1 = 0, int param2 = 0, int param3 = 0, int param4 = 0);
    private static readonly ExecuteCommandDelegate ExecuteCommand;

    private static readonly TaskHelper TaskHelper = new() { TimeLimitMS = int.MaxValue };
    internal static PathFindHelper PathFindHelper = new();
    internal static Task? PathFindTask;

    static GameFunctions()
    {
        ExecuteCommand = Marshal.GetDelegateForFunctionPointer<ExecuteCommandDelegate>(ExecuteCommandSig.ScanText());
    }

    public static unsafe void RegisterToEnterDuty()
        => SendEvent(AgentId.ContentsFinder, 0, 12, 0);

    public static unsafe void Teleport(Vector3 pos)
    {
        if (DService.ClientState.LocalPlayer is not { } localPlayer) return;
        localPlayer.ToStruct()->SetPosition(pos.X, pos.Y, pos.Z);
    }

    /// <summary>
    /// 寻路到目标位置
    /// </summary>
    public static void PathFindStart(Vector3 pos)
    {
        PathFindCancel();

        TaskHelper.Enqueue(() =>
        {
            PathFindTask ??= Task.Run(() => PathFindInternalTask(pos));
            return PathFindTask.IsCompleted;
        });
        TaskHelper.Enqueue(() => PathFindTask = null);
    }

    /// <summary>
    /// 取消寻路
    /// </summary>
    public static void PathFindCancel()
    {
        TaskHelper.Abort();

        PathFindTask?.Dispose();
        PathFindTask = null;

        PathFindHelper.Enabled = false;
        PathFindHelper.DesiredPosition = default;
    }

    private static async void PathFindInternalTask(Vector3 targetPos)
    {
        PathFindHelper.DesiredPosition = targetPos;
        PathFindHelper.Enabled = true;

        while (true)
        {
            var localPlayer = DService.ClientState.LocalPlayer;
            if (localPlayer == null) continue;

            var distance = Vector3.DistanceSquared(localPlayer.Position, targetPos);
            if (distance <= 2) break;

            await Task.Delay(500);
        }

        PathFindHelper.Enabled = false;
        PathFindHelper.DesiredPosition = default;
    }

    public static void LeaveDuty() => ExecuteCommand(819, DService.Condition[ConditionFlag.InCombat] ? 1 : 0);
}
