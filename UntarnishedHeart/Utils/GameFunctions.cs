using System.Numerics;
using Dalamud.Game.ClientState.Conditions;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using OmenTools.Dalamud;
using OmenTools.Info.Game.Enums;
using OmenTools.Interop.Game;
using OmenTools.Interop.Windows.Helpers;
using OmenTools.OmenService;
using OmenTools.Threading;
using OmenTools.Threading.TaskHelper;
using Task = System.Threading.Tasks.Task;

namespace UntarnishedHeart.Utils;

public static class GameFunctions
{
    private static TaskHelper? TaskHelper;

    internal static MovementInputController? MovementInputController;

    internal static Task?                    PathFindTask;
    internal static CancellationTokenSource? PathFindCancelSource;

    private static PathMoveMode LastMoveMode = PathMoveMode.None;
    private static Vector3      LastTargetPos;
    private static bool         LastFly;

    public static void Init()
    {
        MovementInputController ??= new();

        TaskHelper ??= new() { TimeoutMS = int.MaxValue };
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

        MovementInputController?.Dispose();
        MovementInputController = null;
    }

    public static unsafe void Teleport(Vector3 pos)
    {
        TaskHelper.Abort();
        TaskHelper.Enqueue
        (() =>
            {
                if (DService.Instance().ObjectTable.LocalPlayer is not { } localPlayer) return false;
                localPlayer.ToStruct()->SetPosition(pos.X, pos.Y, pos.Z);
                KeyEmulationHelper.SendKeypress(Keys.W);
                return true;
            }
        );
    }

    /// <summary>
    ///     寻路到目标位置（使用 PathFindHelper）
    /// </summary>
    public static void PathFindStart(Vector3 pos)
    {
        LastMoveMode  = PathMoveMode.PathFindHelper;
        LastTargetPos = pos;
        LastFly       = false;

        if (MovementInputController is null)
        {
            NotifyHelper.Instance().Chat("PathFindHelper未初始化");
            return;
        }

        PathFindCancel();
        PathFindCancelSource = new();
        TaskHelper.Enqueue
        (() =>
            {
                if (!Throttler.Shared.Throttle("寻路节流")) return false;

                PathFindTask ??= DService.Instance().Framework.RunOnTick
                (
                    async () => await Task.Run(async () => await PathFindInternalTask(pos), PathFindCancelSource.Token),
                    TimeSpan.Zero,
                    0,
                    PathFindCancelSource.Token
                );

                return PathFindTask.IsCompleted;
            }
        );
    }

    private static async Task PathFindInternalTask(Vector3 targetPos)
    {
        if (MovementInputController is null)
            return;

        MovementInputController.DesiredPosition = targetPos;
        MovementInputController.Enabled         = true;

        while (true)
        {
            if (DService.Instance().ObjectTable.LocalPlayer is not { } localPlayer) continue;

            var distance = Vector3.DistanceSquared(localPlayer.Position, targetPos);
            if (distance <= 2) break;

            await Task.Delay(500);
        }

        MovementInputController.Enabled         = false;
        MovementInputController.DesiredPosition = default;
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
        TaskHelper.Enqueue
        (() =>
            {
                if (!Throttler.Shared.Throttle("寻路节流")) return false;

                PathFindTask ??= DService.Instance().Framework.RunOnTick
                (
                    async () => await Task.Run(async () => await NavmeshMoveTask(pos, fly), PathFindCancelSource.Token),
                    TimeSpan.Zero,
                    0,
                    PathFindCancelSource.Token
                );

                return PathFindTask.IsCompleted;
            }
        );
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
            NotifyHelper.Instance().Chat($"取消寻路 Token 时出错: {ex.Message}");
        }

        // Stop PathFinder
        if (MovementInputController != null)
        {
            try
            {
                MovementInputController.Enabled         = false;
                MovementInputController.DesiredPosition = default;
            }
            catch (Exception ex)
            {
                NotifyHelper.Instance().ChatError($"停止 MovementInputController 时出错: {ex.Message}");
            }
        }

        // Stop vnavmesh
        try
        {
            vnavmeshIPC.StopPathfind();
        }
        catch (Exception ex)
        {
            NotifyHelper.Instance().ChatError($"停止 vnavmesh 时出错: {ex.Message}");
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
                NotifyHelper.Instance().Chat("没有可恢复的寻路任务");
                break;
        }
    }

    /// <summary>
    ///     vnavmeshIPC 移动任务
    /// </summary>
    private static async Task NavmeshMoveTask(Vector3 targetPos, bool fly)
    {
        // 等待 vnavmesh 准备就绪
        var timeout = DateTime.Now.AddSeconds(10);
        while (!vnavmeshIPC.GetIsNavReady() && DateTime.Now < timeout)
            await Task.Delay(100);

        if (!vnavmeshIPC.GetIsNavReady())
        {
            NotifyHelper.Instance().ChatError("vnavmesh 未准备就绪");
            return;
        }

        if (!vnavmeshIPC.PathfindAndMoveTo(targetPos, fly))
        {
            NotifyHelper.Instance().ChatError("vnavmesh 寻路启动失败");
            return;
        }

        await Task.Delay(500);

        // wait finish pathFind
        while (true)
        {
            if (DService.Instance().ObjectTable.LocalPlayer is not { } localPlayer)
            {
                await Task.Delay(100);
                continue;
            }

            // check whether arrived
            var distance = Vector3.Distance(localPlayer.Position, targetPos);

            if (distance <= 2f)
            {
                vnavmeshIPC.StopPathfind();
                break;
            }

            // check vnav status
            if (!vnavmeshIPC.GetIsPathfindRunning() && !vnavmeshIPC.GetIsNavPathfindInProgress())
            {
                await Task.Delay(500);

                distance = Vector3.Distance(localPlayer.Position, targetPos);
                if (distance <= 2f) break;

                NotifyHelper.Instance().Chat($"vnavmesh 寻路结束但未到达目标，距离: {distance:F2}米");
                break;
            }

            await Task.Delay(100);
        }
    }

    public static unsafe void EquipRecommendGear()
    {
        var instance = RecommendEquipModule.Instance();

        instance->SetupForClassJob((byte)LocalPlayerState.ClassJob);

        DService.Instance().Framework.RunOnTick(() => instance->EquipRecommendedGear(), TimeSpan.FromMilliseconds(100));
    }

    public static void LeaveDuty() =>
        ExecuteCommandManager.Instance().ExecuteCommand(ExecuteCommandFlag.LeaveDuty, DService.Instance().Condition[ConditionFlag.InCombat] ? 1U : 0);
}

internal enum PathMoveMode
{
    None,
    PathFindHelper,
    vnavmesh
}
