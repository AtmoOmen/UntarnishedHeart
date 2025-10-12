using System;
using System.Numerics;
using System.Threading;
using System.Windows.Forms;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using UntarnishedHeart.Managers;
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
    internal static PathFindHelper? PathFinder;
    internal static Task? PathFindTask;
    internal static CancellationTokenSource? PathFindCancelSource;
    
    public static PathFindMode CurrentPathFindMode { get; set; } = PathFindMode.VNavmesh;

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
            DService.Log.Error($"PathFindHelper初始化失败: {ex.Message}");
        }

        // init VNavmesh
        VNavmesh ??= new(DService.PI);
        if (VNavmesh is { IsAvailable: true })
        {
            DService.Log.Info("vnavmesh IPC 初始化成功");
        }
        else
        {
            DService.Log.Warning("vnavmesh 不可用");
        }

        CurrentPathFindMode = Service.Config.PathFindMode;
        DService.Log.Info($"当前寻路模式: {CurrentPathFindMode}");

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
    /// 寻路到目标位置
    /// </summary>
    public static void PathFindStart(Vector3 pos, bool fly = false)
    {
        PathFindCancel();
        
        switch (CurrentPathFindMode)
        {
            case PathFindMode.Native:
                PathFindStartNative(pos);
                break;

            case PathFindMode.VNavmesh:
                PathFindStartVNavmesh(pos, fly);
                break;

            default:
                DService.Log.Error($"未知的寻路模式: {CurrentPathFindMode}");
                break;
        }
    }

    /// <summary>
    /// 使用 PathFindHelper 寻路
    /// </summary>
    private static void PathFindStartNative(Vector3 pos)
    {
        if (PathFinder == null)
        {
            DService.Log.Error("PathFindHelper未初始化");
            return;
        }

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
        if (PathFinder == null)
            return;

        PathFinder.DesiredPosition = targetPos;
        PathFinder.Enabled = true;

        while (true)
        {
            var localPlayer = DService.ObjectTable.LocalPlayer;
            if (localPlayer == null) continue;

            var distance = Vector3.DistanceSquared(localPlayer.Position, targetPos);
            if (distance <= 2) break;

            await Task.Delay(500);
        }

        PathFinder.Enabled = false;
        PathFinder.DesiredPosition = default;
    }

    /// <summary>
    /// 使用 VNavmesh 寻路
    /// </summary>
    private static void PathFindStartVNavmesh(Vector3 pos, bool fly)
    {
        if (VNavmesh is not { IsAvailable: true })
        {
            DService.Log.Error("vnavmesh 不可用，无法寻路");
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
        
        try
        {
            PathFindCancelSource?.Cancel();
        }
        catch (Exception ex)
        {
            DService.Log.Debug($"取消寻路 Token 时出错: {ex.Message}");
        }
        
        switch (CurrentPathFindMode)
        {
            case PathFindMode.Native:
                if (PathFinder != null)
                {
                    try
                    {
                        PathFinder.Enabled = false;
                        PathFinder.DesiredPosition = default;
                    }
                    catch (Exception ex)
                    {
                        DService.Log.Debug($"停止 PathFinder 时出错: {ex.Message}");
                    }
                }
                break;

            case PathFindMode.VNavmesh:
                try
                {
                    VNavmesh?.PathStop();
                }
                catch (Exception ex)
                {
                    DService.Log.Debug($"停止 vnavmesh 时出错: {ex.Message}");
                }
                break;
        }
        
        PathFindCancelSource?.Dispose();
        PathFindCancelSource = null;
        PathFindTask = null;
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
            DService.Log.Warning("vnavmesh 未准备就绪");
            return;
        }
        
        if (!VNavmesh.PathfindAndMoveTo(targetPos, fly))
        {
            DService.Log.Warning("vnavmesh 寻路启动失败");
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
            
            // check whether arrived
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
                
                DService.Log.Warning($"vnavmesh 寻路结束但未到达目标，距离: {distance:F2}米");
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

public enum PathFindMode
{
    /// <summary>
    /// PathFindHelper
    /// </summary>
    Native = 0,

    /// <summary>
    /// VNavmesh
    /// </summary>
    VNavmesh = 1
}
