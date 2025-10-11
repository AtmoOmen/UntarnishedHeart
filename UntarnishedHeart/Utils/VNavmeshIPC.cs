using System;
using System.Numerics;
using Dalamud.Plugin;
using Dalamud.Plugin.Ipc;

namespace UntarnishedHeart.Utils;

/// <summary>
/// VNavmesh IPC 封装
/// </summary>
public class VNavmeshIPC : IDisposable
{
    private readonly ICallGateSubscriber<bool> isReady = null!;
    private readonly ICallGateSubscriber<bool> isPathGenerating = null!;
    private readonly ICallGateSubscriber<bool> isPathRunning = null!;
    private readonly ICallGateSubscriber<float> getPathDistance = null!;
    private readonly ICallGateSubscriber<Vector3, bool, bool> pathfindAndMoveTo = null!;
    private readonly ICallGateSubscriber<bool, object> pathStop = null!;

    public bool IsAvailable { get; private set; }

    /// <summary>
    /// 初始化 VNavmesh IPC
    /// </summary>
    /// <param name="pi">Dalamud Plugin Interface</param>
    public VNavmeshIPC(IDalamudPluginInterface pi)
    {
        try
        {
            // subs VNavmesh's IPC 
            isReady = pi.GetIpcSubscriber<bool>("vnavmesh.Nav.IsReady");
            isPathGenerating = pi.GetIpcSubscriber<bool>("vnavmesh.Path.IsGenerating");
            isPathRunning = pi.GetIpcSubscriber<bool>("vnavmesh.Path.IsRunning");
            getPathDistance = pi.GetIpcSubscriber<float>("vnavmesh.Path.GetDistance");
            pathfindAndMoveTo = pi.GetIpcSubscriber<Vector3, bool, bool>("vnavmesh.SimpleMove.PathfindAndMoveTo");
            pathStop = pi.GetIpcSubscriber<bool, object>("vnavmesh.Path.Stop");

            IsAvailable = true;
        }
        catch (Exception ex)
        {
            IsAvailable = false;
            DService.Log.Debug($"VNavmesh IPC 初始化失败: {ex.Message}");
        }
    }

    /// <summary>
    /// VNavmesh 是否准备就绪
    /// </summary>
    public bool IsReady()
    {
        if (!IsAvailable) return false;
        try
        {
            return isReady.InvokeFunc();
        }
        catch (Exception ex)
        {
            IsAvailable = false;
            DService.Log.Debug($"VNavmesh IsReady 调用失败: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 是否正在生成路径
    /// </summary>
    public bool IsPathGenerating()
    {
        if (!IsAvailable) return false;
        try
        {
            return isPathGenerating.InvokeFunc();
        }
        catch (Exception ex)
        {
            DService.Log.Debug($"VNavmesh IsPathGenerating 调用失败: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 是否正在沿路径移动
    /// </summary>
    public bool IsPathRunning()
    {
        if (!IsAvailable) return false;
        try
        {
            return isPathRunning.InvokeFunc();
        }
        catch (Exception ex)
        {
            DService.Log.Debug($"VNavmesh IsPathRunning 调用失败: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 获取路径距离
    /// </summary>
    public float GetPathDistance()
    {
        if (!IsAvailable) return 0;
        try
        {
            return getPathDistance.InvokeFunc();
        }
        catch (Exception ex)
        {
            DService.Log.Debug($"VNavmesh GetPathDistance 调用失败: {ex.Message}");
            return 0;
        }
    }

    /// <summary>
    /// 寻路并移动到目标位置（简单模式）
    /// </summary>
    /// <param name="target">目标位置</param>
    /// <param name="fly">是否飞行</param>
    /// <returns>是否成功开始寻路</returns>
    public bool PathfindAndMoveTo(Vector3 target, bool fly = false)
    {
        if (!IsAvailable || !IsReady()) return false;
        try
        {
            return pathfindAndMoveTo.InvokeFunc(target, fly);
        }
        catch (Exception ex)
        {
            DService.Log.Warning($"VNavmesh PathfindAndMoveTo 调用失败: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 停止寻路
    /// </summary>
    public void PathStop()
    {
        if (!IsAvailable) return;
        try
        {
            pathStop.InvokeFunc(true);
        }
        catch (Exception ex)
        {
            DService.Log.Debug($"VNavmesh PathStop 调用失败: {ex.Message}");
        }
    }


    public void Dispose()
    {
        PathStop();
    }
}
