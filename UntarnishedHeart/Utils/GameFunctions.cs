using System;
using System.Numerics;
using System.Threading;
using System.Windows.Forms;
using Dalamud.Game.ClientState.Conditions;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using OmenTools.Managers;
using UntarnishedHeart.Windows;
using Task = System.Threading.Tasks.Task;

namespace UntarnishedHeart.Utils;

public static class GameFunctions
{
    private static TaskHelper? TaskHelper;

    internal static PathFindHelper?          PathFinder;
    internal static Task?                    PathFindTask;
    internal static CancellationTokenSource? PathFindCancelSource;

    private static PathMoveMode LastMoveMode = PathMoveMode.None;
    private static Vector3      LastTargetPos;
    private static bool         LastFly;

    public static void Init()
    {
        PathFinder ??= new();
        vnavmeshIPC.Init();

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

        vnavmeshIPC.Uninit();

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
    ///     寻路到目标位置（使用 PathFindHelper）
    /// </summary>
    public static void PathFindStart(Vector3 pos)
    {
        LastMoveMode  = PathMoveMode.PathFindHelper;
        LastTargetPos = pos;
        LastFly       = false;
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
        PathFinder.Enabled         = true;

        while (true)
        {
            if (DService.ObjectTable.LocalPlayer is not { } localPlayer) continue;

            var distance = Vector3.DistanceSquared(localPlayer.Position, targetPos);
            if (distance <= 2) break;

            await Task.Delay(500);
        }

        PathFinder.Enabled         = false;
        PathFinder.DesiredPosition = default;
    }

    /// <summary>
    ///     寻路到目标位置（使用 vnavmesh）
    /// </summary>
    public static void vnavmeshMove(Vector3 pos, bool fly = false)
    {
        LastMoveMode  = PathMoveMode.vnavmesh;
        LastTargetPos = pos;
        LastFly       = fly;

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
    ///     取消寻路/移动
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
        if (PathFinder != null)
        {
            try
            {
                PathFinder.Enabled         = false;
                PathFinder.DesiredPosition = default;
            }
            catch (Exception ex)
            {
                Chat($"停止 PathFinder 时出错: {ex.Message}", Main.UTHPrefix);
            }
        }

        // Stop vnavmesh
        try
        {
            vnavmeshIPC.PathStop();
        }
        catch (Exception ex)
        {
            Chat($"停止 vnavmesh 时出错: {ex.Message}", Main.UTHPrefix);
        }

        PathFindCancelSource?.Dispose();
        PathFindCancelSource = null;
        PathFindTask         = null;
    }

    /// <summary>
    ///     继续上一次的寻路/移动
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
    ///     vnavmeshIPC 移动任务
    /// </summary>
    private static async Task vnavmeshMoveTask(Vector3 targetPos, bool fly)
    {
        // 等待 vnavmesh 准备就绪
        var timeout = DateTime.Now.AddSeconds(10);
        while (!vnavmeshIPC.NavIsReady() && DateTime.Now < timeout)
            await Task.Delay(100);

        if (!vnavmeshIPC.NavIsReady())
        {
            Chat("vnavmesh 未准备就绪", Main.UTHPrefix);
            return;
        }

        if (!vnavmeshIPC.PathfindAndMoveTo(targetPos, fly))
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
                vnavmeshIPC.PathStop();
                break;
            }

            // check vnav status
            if (!vnavmeshIPC.PathIsRunning() && !vnavmeshIPC.PathIsGenerating())
            {
                await Task.Delay(500);

                distance = Vector3.Distance(localPlayer.Position, targetPos);
                if (distance <= 2f) break;

                Chat($"vnavmesh 寻路结束但未到达目标，距离: {distance:F2}米", Main.UTHPrefix);
                break;
            }

            await Task.Delay(100);
        }
    }

    public static unsafe void EquipRecommendGear()
    {
        var instance = RecommendEquipModule.Instance();

        instance->SetupForClassJob((byte)LocalPlayerState.ClassJob);

        DService.Framework.RunOnTick(() => instance->EquipRecommendedGear(), TimeSpan.FromMilliseconds(100));
    }

    public static void LeaveDuty() =>
        ExecuteCommandManager.ExecuteCommand(ExecuteCommandFlag.LeaveDuty, DService.Condition[ConditionFlag.InCombat] ? 1U : 0);
}

internal enum PathMoveMode
{
    None,
    PathFindHelper,
    vnavmesh
}
