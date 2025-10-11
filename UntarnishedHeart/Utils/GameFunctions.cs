using System;
using System.Numerics;
using System.Threading;
using System.Windows.Forms;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using Task = System.Threading.Tasks.Task;

namespace UntarnishedHeart.Utils;

public static class GameFunctions
{
    private static readonly CompSig ExecuteCommandSig = 
        new("E8 ?? ?? ?? ?? 48 8B 5C 24 ?? 48 8B 74 24 ?? 48 83 C4 ?? 5F C3 CC CC CC CC CC CC CC CC CC CC 48 89 5C 24 ?? 57 48 83 EC ?? 80 A1");
    private delegate nint                    ExecuteCommandDelegate(ExecuteCommandFlag command, uint param1 = 0, uint param2 = 0, uint param3 = 0, uint param4 = 0);
    private static   ExecuteCommandDelegate? ExecuteCommand;

    private static TaskHelper? TaskHelper;

    internal static VNavmeshIPC? VNavmesh;
    internal static Task? PathFindTask;
    internal static CancellationTokenSource? PathFindCancelSource;

    public static void Init()
    {
        ExecuteCommand ??= ExecuteCommandSig.GetDelegate<ExecuteCommandDelegate>();
        VNavmesh ??= new(DService.PI);

        if (!VNavmesh.IsAvailable)
        {
            NotifyHelper.NotificationError("vnavmesh 不可用\n插件寻路功能需要安装 vnavmesh 插件才能使用。");
            DService.Log.Error("vnavmesh 不可用");
        }
        else
        {
            DService.Log.Info("vnavmesh IPC 初始化成功");
        }

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

        VNavmesh?.Dispose();
        VNavmesh = null;
    }

    public static unsafe void Teleport(Vector3 pos)
    {
        TaskHelper.Abort();
        TaskHelper.Enqueue(() =>
        {
            if (DService.ObjectTable.LocalPlayer is not { } localPlayer) return false;
            localPlayer.ToStruct()->SetPosition(pos.X, pos.Y, pos.Z);
            SendKeypress(Keys.W);
            return true;
        });
    }

    /// <summary>
    /// 寻路到目标位置
    /// </summary>
    public static void PathFindStart(Vector3 pos, bool fly = false)
    {
        PathFindCancel();

        if (VNavmesh is not { IsAvailable: true })
        {
            DService.Log.Error("vnavmesh 不可用，无法寻路。请安装 vnavmesh 插件。");
            return;
        }

        PathFindCancelSource = new();
        TaskHelper.Enqueue(() =>
        {
            if (!Throttler.Throttle("寻路节流")) return false;

            PathFindTask ??= DService.Framework.RunOnTick(
                async () => await Task.Run(async () => await PathFindVNavmeshTask(pos, fly), PathFindCancelSource.Token),
                TimeSpan.Zero, 0, PathFindCancelSource.Token);

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

        VNavmesh?.PathStop();
    }

    /// <summary>
    /// 使用 vnavIPC 寻路
    /// </summary>
    private static async Task PathFindVNavmeshTask(Vector3 targetPos, bool fly)
    {
        if (VNavmesh == null || !VNavmesh.IsAvailable)
            return;

        // 等待 VNavmesh 准备就绪
        var timeout = DateTime.Now.AddSeconds(10);
        while (!VNavmesh.IsReady() && DateTime.Now < timeout)
        {
            await Task.Delay(100);
        }

        if (!VNavmesh.IsReady())
        {
            DService.Log.Warning("VNavmesh 未准备就绪");
            return;
        }
        
        if (!VNavmesh.PathfindAndMoveTo(targetPos, fly))
        {
            DService.Log.Warning("VNavmesh 寻路启动失败");
            return;
        }

        // wait finish pathFind
        while (true)
        {
            var localPlayer = DService.ObjectTable.LocalPlayer;
            if (localPlayer == null)
            {
                await Task.Delay(100);
                continue;
            }
            
            // check whether arrived(2m)
            var distance = Vector3.Distance(localPlayer.Position, targetPos);
            if (distance <= 2f)
            {
                VNavmesh.PathStop();
                break;
            }

            // check vnav status
            if (!VNavmesh.IsPathRunning() && !VNavmesh.IsPathGenerating())
            {
                await Task.Delay(500);
                
                distance = Vector3.Distance(localPlayer.Position, targetPos);
                if (distance <= 2f)
                {
                    break;
                }
                
                DService.Log.Warning($"VNavmesh 寻路结束但未到达目标，距离: {distance:F2}米");
                break;
            }

            await Task.Delay(100);
        }
    }

    public static unsafe void EquipRecommendGear()
    {
        var instance = RecommendEquipModule.Instance();

        instance->SetupForClassJob((byte)(DService.ClientState.LocalPlayer?.ClassJob.RowId ?? 0));

        DService.Framework.RunOnTick(() => instance->EquipRecommendedGear(), TimeSpan.FromMilliseconds(100));
    }

    public static void LeaveDuty() => 
        ExecuteCommand(ExecuteCommandFlag.LeaveDuty, DService.Condition[ConditionFlag.InCombat] ? 1U : 0);
}
