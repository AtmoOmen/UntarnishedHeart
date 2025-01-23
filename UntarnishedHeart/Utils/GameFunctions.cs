using System.Numerics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Dalamud.Game.ClientState.Conditions;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;

namespace UntarnishedHeart.Utils;

public static class GameFunctions
{
    private static readonly CompSig ExecuteCommandSig = 
        new("E8 ?? ?? ?? ?? 48 8B 5C 24 ?? 48 8B 74 24 ?? 48 83 C4 ?? 5F C3 CC CC CC CC CC CC CC CC CC CC 48 89 5C 24 ?? 57 48 83 EC ?? 80 A1");
    private delegate nint ExecuteCommandDelegate(int command, int param1 = 0, int param2 = 0, int param3 = 0, int param4 = 0);
    private static ExecuteCommandDelegate? ExecuteCommand;

    private static TaskHelper? TaskHelper;

    internal static PathFindHelper? PathFindHelper;
    internal static Task? PathFindTask;
    internal static CancellationTokenSource? PathFindCancelSource;

    public static void Init()
    {
        ExecuteCommand ??= Marshal.GetDelegateForFunctionPointer<ExecuteCommandDelegate>(ExecuteCommandSig.ScanText());
        PathFindHelper ??= new();
        TaskHelper ??= new() { TimeLimitMS = int.MaxValue };
    }

    public static void Uninit()
    {
        TaskHelper?.Abort();
        TaskHelper?.Dispose();
        TaskHelper = null;

        PathFindCancelSource?.Cancel();
        PathFindCancelSource?.Dispose();
        PathFindCancelSource = null;

        PathFindTask?.Dispose();
        PathFindTask = null;

        PathFindHelper.Dispose();
    }

    public static unsafe void RegisterToEnterDuty(bool isHighEnd = false)
    {
        if (isHighEnd)
            SendEvent(AgentId.RaidFinder, 81, 11);
        else
            SendEvent(AgentId.ContentsFinder, 0, 12, 0);
    }

    public static unsafe void Teleport(Vector3 pos)
    {
        TaskHelper.Abort();
        TaskHelper.Enqueue(() =>
        {
            if (DService.ClientState.LocalPlayer is not { } localPlayer) return false;
            localPlayer.ToStruct()->SetPosition(pos.X, pos.Y, pos.Z);
            SendKeypress(Keys.W);
            return true;
        });
    }

    /// <summary>
    /// 寻路到目标位置
    /// </summary>
    public static void PathFindStart(Vector3 pos)
    {
        PathFindCancel();

        PathFindCancelSource = new();
        TaskHelper.Enqueue(() =>
        {
            if (!Throttler.Throttle("寻路节流")) return false;

            PathFindTask ??= DService.Framework.RunOnTick(
                async () => await Task.Run(() => PathFindInternalTask(pos), PathFindCancelSource.Token), 
                default, 0, PathFindCancelSource.Token);

            return PathFindTask.IsCompleted;
        });
    }

    /// <summary>
    /// 取消寻路
    /// </summary>
    public static void PathFindCancel()
    {
        TaskHelper?.Abort();

        PathFindCancelSource?.Cancel();
        PathFindCancelSource?.Dispose();
        PathFindCancelSource = null;

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
