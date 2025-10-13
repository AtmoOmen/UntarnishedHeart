using System;
using System.Numerics;
using Dalamud.Plugin;
using Dalamud.Plugin.Ipc;

namespace UntarnishedHeart.Utils;

/// <summary>
/// vnavmesh IPC
/// </summary>
public class vnavmeshIPC : IDisposable
{
    private readonly ICallGateSubscriber<bool> isReady = null!;
    private readonly ICallGateSubscriber<bool> isPathGenerating = null!;
    private readonly ICallGateSubscriber<bool> isPathRunning = null!;
    private readonly ICallGateSubscriber<float> getPathDistance = null!;
    private readonly ICallGateSubscriber<Vector3, bool, bool> pathfindAndMoveTo = null!;
    private readonly ICallGateSubscriber<object> pathStop = null!;

    public bool IsAvailable { get; private set; }

    /// <summary>
    /// Init vnavmesh IPC
    /// </summary>
    /// <param name="pi">Dalamud Plugin Interface</param>
    public vnavmeshIPC(IDalamudPluginInterface pi)
    {
        try
        {
            // subs vnavmesh's IPC
            isReady = pi.GetIpcSubscriber<bool>("vnavmesh.Nav.IsReady");
            isPathGenerating = pi.GetIpcSubscriber<bool>("vnavmesh.Path.IsGenerating");
            isPathRunning = pi.GetIpcSubscriber<bool>("vnavmesh.Path.IsRunning");
            getPathDistance = pi.GetIpcSubscriber<float>("vnavmesh.Path.GetDistance");
            pathfindAndMoveTo = pi.GetIpcSubscriber<Vector3, bool, bool>("vnavmesh.SimpleMove.PathfindAndMoveTo");
            pathStop = pi.GetIpcSubscriber<object>("vnavmesh.Path.Stop");
            
            try
            {
                isReady.InvokeFunc();
                IsAvailable = true;
            }
            catch
            {
                IsAvailable = false;
                NotifyHelper.NotificationError("vnavmesh 未运行或不可用");
            }
        }
        catch (Exception ex)
        {
            IsAvailable = false;
            NotifyHelper.NotificationError($"vnavmesh IPC 初始化失败: {ex.Message}");
        }
    }
 
    /// <summary>
    /// vnavmesh 是否准备就绪
    /// </summary>
    public bool IsReady()
    {
        try
        {
            var result = isReady.InvokeFunc();
            IsAvailable = true;
            return result;
        }
        catch (Exception ex)
        {
            IsAvailable = false;
            NotifyHelper.NotificationError($"vnavmesh IsReady 调用失败: {ex.Message}");
            return false;
        }
    }
    
    public bool IsPathGenerating()
    {
        if (!IsAvailable) return false;
        try
        {
            if (!IsReady()) return false;
            return isPathGenerating.InvokeFunc();
        }
        catch (Exception ex)
        {
            NotifyHelper.NotificationWarning($"vnavmesh IsPathGenerating 调用失败: {ex.Message}");
            return false;
        }
    }
    
    public bool IsPathRunning()
    {
        if (!IsAvailable) return false;
        try
        {
            if (!IsReady()) return false;
            return isPathRunning.InvokeFunc();
        }
        catch (Exception ex)
        {
            NotifyHelper.NotificationWarning($"vnavmesh IsPathRunning 调用失败: {ex.Message}");
            return false;
        }
    }

    public float GetPathDistance()
    {
        if (!IsAvailable) return 0;
        try
        {
            if (!IsReady()) return 0;
            return getPathDistance.InvokeFunc();
        }
        catch (Exception ex)
        {
            NotifyHelper.NotificationWarning($"vnavmesh GetPathDistance 调用失败: {ex.Message}");
            return 0;
        }
    }

    /// <summary>
    /// 寻路并移动到目标位置
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
            NotifyHelper.NotificationWarning($"vnavmesh PathfindAndMoveTo 调用失败: {ex.Message}");
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
            pathStop.InvokeAction();
        }
        catch (Exception ex)
        {
            NotifyHelper.NotificationWarning($"vnavmesh Path.Stop IPC 调用失败: {ex.Message}");
        }
    }
    
    public void Dispose()
    {
        PathStop();
    }
}
