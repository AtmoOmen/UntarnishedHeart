using System;
using System.Numerics;
using System.Threading;
using System.Windows.Forms;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using UntarnishedHeart.Managers;
using UntarnishedHeart.Windows;
using Task = System.Threading.Tasks.Task;

namespace UntarnishedHeart.Utils;

public static class GameFunctions
{
    private static readonly CompSig ExecuteCommandSig = 
        new("E8 ?? ?? ?? ?? 48 8B 5C 24 ?? 48 8B 74 24 ?? 48 83 C4 ?? 5F C3 CC CC CC CC CC CC CC CC CC CC 48 89 5C 24 ?? 57 48 83 EC ?? 80 A1");
    private delegate nint                    ExecuteCommandDelegate(ExecuteCommandFlag command, uint param1 = 0, uint param2 = 0, uint param3 = 0, uint param4 = 0);
    private static   ExecuteCommandDelegate? ExecuteCommand;

    private static TaskHelper? TaskHelper;
    
    internal static vnavmeshIPC?             vnavmesh;
    internal static PathFindHelper?          PathFinder;
    internal static Task?                    PathFindTask;
    internal static CancellationTokenSource? PathFindCancelSource;

    private static PathMoveMode LastMoveMode = PathMoveMode.None;
    private static Vector3      LastTargetPos;
    private static bool         LastFly;

    public static void Init()
    {
        ExecuteCommand ??= ExecuteCommandSig.GetDelegate<ExecuteCommandDelegate>();

        // init PathFindHelper
        try
        {
            PathFinder ??= new PathFindHelper();
        }
        catch (Exception ex)
        {
            Chat($"PathFindHelper初始化失败: {ex.Message}", Main.UTHPrefix);
        }

        // init vnavmesh
        vnavmesh ??= new(DService.PI);
        if (vnavmesh is { IsAvailable: true })
        {
            Chat($"vnavmesh IPC 初始化成功", Main.UTHPrefix);
        }
        else
        {
            Chat("vnavmesh 不可用", Main.UTHPrefix);
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

        vnavmesh?.Dispose();
        vnavmesh = null;

        PathFinder?.Dispose();
        PathFinder = null;
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
    /// 寻路到目标位置（使用 PathFindHelper）
    /// </summary>
    public static void PathFindStart(Vector3 pos)
    {
        LastMoveMode = PathMoveMode.PathFindHelper;
        LastTargetPos = pos;
        LastFly = false;
        if (PathFinder is null)
        {
            Chat("PathFindHelper未初始化", Main.UTHPrefix);
            return;
        }

        PathFindCancel();
        PathFindCancelSource = new();
        TaskHelper.Enqueue(() =>
        {
            if (!Throttler.Throttle("寻路节流")) return false;

            PathFindTask ??= DService.Framework.RunOnTick(
                async () => await Task.Run(async () => await PathFindInternalTask(pos), PathFindCancelSource.Token),
                TimeSpan.Zero, 0, PathFindCancelSource.Token);

            return PathFindTask.IsCompleted;
        });
    }
    
    private static async Task PathFindInternalTask(Vector3 targetPos)
    {
        if (PathFinder is null)
            return;

        PathFinder.DesiredPosition = targetPos;
        PathFinder.Enabled = true;

        while (true)
        {
            if (DService.ObjectTable.LocalPlayer is not { } localPlayer) continue;

            var distance = Vector3.DistanceSquared(localPlayer.Position, targetPos);
            if (distance <= 2) break;

            await Task.Delay(500);
        }

        PathFinder.Enabled = false;
        PathFinder.DesiredPosition = default;
    }

    /// <summary>
    /// 寻路到目标位置（使用 vnavmesh）
    /// </summary>
    public static void vnavmeshMove(Vector3 pos, bool fly = false)
    {
        LastMoveMode = PathMoveMode.vnavmesh;
        LastTargetPos = pos;
        LastFly = fly;

        if (vnavmesh == null || !vnavmesh.IsReady())
        {
            Chat("vnavmesh 不可用，无法寻路", Main.UTHPrefix);
            return;
        }
        
        PathFindCancel();
        PathFindCancelSource = new();
        TaskHelper.Enqueue(() =>
        {
            if (!Throttler.Throttle("寻路节流")) return false;

            PathFindTask ??= DService.Framework.RunOnTick(
                async () => await Task.Run(async () => await vnavmeshMoveTask(pos, fly), PathFindCancelSource.Token),
                TimeSpan.Zero, 0, PathFindCancelSource.Token);

            return PathFindTask.IsCompleted;
        });
    }

    /// <summary>
    /// 取消寻路/移动
    /// </summary>
    public static void PathFindCancel()
    {
        TaskHelper?.Abort();

        try
        {
            PathFindCancelSource?.Cancel();
        }
        catch (Exception ex)
        {
            Chat($"取消寻路 Token 时出错: {ex.Message}", Main.UTHPrefix);
        }

        // Stop PathFinder
        if (PathFinder is not null)
        {
            try
            {
                PathFinder.Enabled = false;
                PathFinder.DesiredPosition = default;
            }
            catch (Exception ex)
            {
                Chat($"停止 PathFinder 时出错: {ex.Message}", Main.UTHPrefix);
            }
        }

        // Stop vnavmesh
        if (vnavmesh is not null)
        {
            try
            {
                vnavmesh?.PathStop();
            }
            catch (Exception ex)
            {
                Chat($"停止 vnavmesh 时出错: {ex.Message}", Main.UTHPrefix);
            }
        }

        PathFindCancelSource?.Dispose();
        PathFindCancelSource = null;
        PathFindTask = null;
    }

    /// <summary>
    /// 继续上一次的寻路/移动
    /// </summary>
    public static void PathFindResume()
    {
        if (PathFindTask is { IsCompleted: false }) return;

        switch (LastMoveMode)
        {
            case PathMoveMode.PathFindHelper:
                PathFindStart(LastTargetPos);
                break;
            case PathMoveMode.vnavmesh:
                vnavmeshMove(LastTargetPos, LastFly);
                break;
            case PathMoveMode.None:
            default:
                Chat("没有可恢复的寻路任务", Main.UTHPrefix);
                break;
        }
    }

    /// <summary>
    /// vnavmeshIPC 移动任务
    /// </summary>
    private static async Task vnavmeshMoveTask(Vector3 targetPos, bool fly)
    {
        if (vnavmesh == null || !vnavmesh.IsAvailable)
            return;

        // 等待 vnavmesh 准备就绪
        var timeout = DateTime.Now.AddSeconds(10);
        while (!vnavmesh.IsReady() && DateTime.Now < timeout)
        {
            await Task.Delay(100);
        }

        if (!vnavmesh.IsReady())
        {
            Chat("vnavmesh 未准备就绪", Main.UTHPrefix);
            return;
        }

        if (!vnavmesh.PathfindAndMoveTo(targetPos, fly))
        {
            Chat("vnavmesh 寻路启动失败", Main.UTHPrefix);
            return;
        }
        
        await Task.Delay(500);

        // wait finish pathFind
        while (true)
        {
            if (DService.ObjectTable.LocalPlayer is not { } localPlayer)
            {
                await Task.Delay(100);
                continue;
            }

            // check whether arrived
            var distance = Vector3.Distance(localPlayer.Position, targetPos);
            if (distance <= 2f)
            {
                vnavmesh.PathStop();
                break;
            }

            // check vnav status
            if (!vnavmesh.IsPathRunning() && !vnavmesh.IsPathGenerating())
            {
                await Task.Delay(500);

                distance = Vector3.Distance(localPlayer.Position, targetPos);
                if (distance <= 2f)
                {
                    break;
                }

                Chat($"vnavmesh 寻路结束但未到达目标，距离: {distance:F2}米", Main.UTHPrefix);
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

internal enum PathMoveMode
{
    None,
    PathFindHelper,
    vnavmesh,
}
