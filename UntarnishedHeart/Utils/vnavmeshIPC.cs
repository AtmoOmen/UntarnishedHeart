using System.Collections.Generic;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;

namespace UntarnishedHeart.Utils;

public static class vnavmeshIPC
{
    public const string InternalName = "vnavmesh";

    [IPCSubscriber("vnavmesh.Nav.IsReady", DefaultValue = "false")]
    private static IPCSubscriber<bool>? navIsReady;

    [IPCSubscriber("vnavmesh.Nav.BuildProgress", DefaultValue = "0")]
    private static IPCSubscriber<float>? navBuildProgress;

    [IPCSubscriber("vnavmesh.Nav.Reload")]
    private static IPCSubscriber<bool>? navReload;

    [IPCSubscriber("vnavmesh.Nav.Rebuild")]
    private static IPCSubscriber<bool>? navRebuild;

    [IPCSubscriber("vnavmesh.Nav.Pathfind")]
    private static IPCSubscriber<Vector3, Vector3, bool, Task<List<Vector3>>>? navPathfind;

    [IPCSubscriber("vnavmesh.Nav.PathfindCancelable")]
    private static IPCSubscriber<Vector3, Vector3, bool, CancellationToken, Task<List<Vector3>>>? navPathfindCancelable;

    [IPCSubscriber("vnavmesh.Nav.IsAutoLoad", DefaultValue = "false")]
    private static IPCSubscriber<bool>? navIsAutoLoad;

    [IPCSubscriber("vnavmesh.Nav.SetAutoLoad")]
    private static IPCSubscriber<bool, object>? navSetAutoLoad;

    [IPCSubscriber("vnavmesh.Nav.PathfindNumQueued", DefaultValue = "0")]
    private static IPCSubscriber<int>? navPathfindNumQueued;

    [IPCSubscriber("vnavmesh.Nav.BuildBitmap")]
    private static IPCSubscriber<Vector3, string, float, bool>? navBuildBitmap;

    [IPCSubscriber("vnavmesh.Nav.BuildBitmapBounded")]
    private static IPCSubscriber<Vector3, string, float, Vector3, Vector3, bool>? navBuildBitmapBounded;

    [IPCSubscriber("vnavmesh.Query.Mesh.NearestPoint")]
    private static IPCSubscriber<Vector3, float, float, Vector3?>? queryMeshNearestPoint;

    [IPCSubscriber("vnavmesh.Query.Mesh.PointOnFloor")]
    private static IPCSubscriber<Vector3, bool, float, Vector3?>? queryMeshPointOnFloor;

    [IPCSubscriber("vnavmesh.Path.MoveTo")]
    private static IPCSubscriber<List<Vector3>, bool, object>? pathMoveTo;

    [IPCSubscriber("vnavmesh.Path.Stop")]
    private static IPCSubscriber<object>? pathStop;

    [IPCSubscriber("vnavmesh.Path.IsRunning", DefaultValue = "false")]
    private static IPCSubscriber<bool>? pathIsRunning;

    [IPCSubscriber("vnavmesh.Path.NumWaypoints", DefaultValue = "0")]
    private static IPCSubscriber<int>? pathNumWaypoints;

    [IPCSubscriber("vnavmesh.Path.ListWaypoints")]
    private static IPCSubscriber<List<Vector3>>? pathListWaypoints;

    [IPCSubscriber("vnavmesh.Path.GetMovementAllowed", DefaultValue = "false")]
    private static IPCSubscriber<bool>? pathGetMovementAllowed;

    [IPCSubscriber("vnavmesh.Path.SetMovementAllowed")]
    private static IPCSubscriber<bool, object>? pathSetMovementAllowed;

    [IPCSubscriber("vnavmesh.Path.GetAlignCamera", DefaultValue = "false")]
    private static IPCSubscriber<bool>? pathGetAlignCamera;

    [IPCSubscriber("vnavmesh.Path.SetAlignCamera")]
    private static IPCSubscriber<bool, object>? pathSetAlignCamera;

    [IPCSubscriber("vnavmesh.Path.GetTolerance", DefaultValue = "0")]
    private static IPCSubscriber<float>? pathGetTolerance;

    [IPCSubscriber("vnavmesh.Path.SetTolerance")]
    private static IPCSubscriber<float, object>? pathSetTolerance;

    [IPCSubscriber("vnavmesh.SimpleMove.PathfindAndMoveTo")]
    private static IPCSubscriber<Vector3, bool, bool>? pathfindAndMoveTo;

    [IPCSubscriber("vnavmesh.SimpleMove.PathfindInProgress", DefaultValue = "false")]
    private static IPCSubscriber<bool>? pathfindInProgress;

    [IPCSubscriber("vnavmesh.Nav.PathfindInProgress", DefaultValue = "false")]
    private static IPCSubscriber<bool>? navPathfindInProgress;

    [IPCSubscriber("vnavmesh.Nav.PathfindCancelAll")]
    private static IPCSubscriber<object>? pathfindCancelAll;

    [IPCSubscriber("vnavmesh.Window.IsOpen", DefaultValue = "false")]
    private static IPCSubscriber<bool>? windowIsOpen;

    [IPCSubscriber("vnavmesh.Window.SetOpen")]
    private static IPCSubscriber<bool, object>? windowSetOpen;

    [IPCSubscriber("vnavmesh.DTR.IsShown", DefaultValue = "false")]
    private static IPCSubscriber<bool>? dtrIsShown;

    [IPCSubscriber("vnavmesh.DTR.SetShown")]
    private static IPCSubscriber<bool, object>? dtrSetShown;

    [IPCSubscriber("vnavmesh.Path.IsGenerating", DefaultValue = "false")]
    private static IPCSubscriber<bool>? pathIsGenerating;

    [IPCSubscriber("vnavmesh.Path.GetDistance", DefaultValue = "0")]
    private static IPCSubscriber<float>? pathGetDistance;

    /// <summary>
    ///     检查剩余路径距离
    /// </summary>
    public static float PathGetDistance() =>
        pathGetDistance ?? 0f;

    /// <summary>
    ///     检查是否正在生成导航路径
    /// </summary>
    public static bool PathIsGenerating() =>
        pathIsGenerating ?? false;

    /// <summary>
    ///     检查导航网格是否准备就绪
    /// </summary>
    /// <returns></returns>
    public static bool NavIsReady() =>
        navIsReady ?? false;

    /// <summary>
    ///     获取导航网格的构建进度
    /// </summary>
    /// <returns></returns>
    public static float NavBuildProgress() =>
        navBuildProgress ?? 0f;

    /// <summary>
    ///     重新加载导航网格
    /// </summary>
    public static void NavReload() =>
        navReload?.InvokeFunc();

    /// <summary>
    ///     重新构建导航网格
    /// </summary>
    public static void NavRebuild() =>
        navRebuild?.InvokeFunc();

    /// <summary>
    ///     寻路
    /// </summary>
    /// <param name="from">起点</param>
    /// <param name="to">终点</param>
    /// <param name="fly">是否飞行</param>
    /// <returns></returns>
    public static Task<List<Vector3>>? NavPathfind(Vector3 from, Vector3 to, bool fly = false) =>
        navPathfind?.InvokeFunc(from, to, fly);

    /// <summary>
    ///     可取消的寻路
    /// </summary>
    /// <param name="from">起点</param>
    /// <param name="to">终点</param>
    /// <param name="fly">是否飞行</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns></returns>
    public static Task<List<Vector3>>? NavPathfindCancelable(Vector3 from, Vector3 to, bool fly, CancellationToken cancellationToken) =>
        navPathfindCancelable?.InvokeFunc(from, to, fly, cancellationToken);

    /// <summary>
    ///     获取排队的寻路请求数量
    /// </summary>
    /// <returns></returns>
    public static int NavPathfindNumQueued() =>
        navPathfindNumQueued ?? 0;

    /// <summary>
    ///     构建导航网格的位图表示
    /// </summary>
    /// <param name="startingPos">起始位置</param>
    /// <param name="filename">文件名</param>
    /// <param name="pixelSize">像素大小</param>
    /// <returns></returns>
    public static bool NavBuildBitmap(Vector3 startingPos, string filename, float pixelSize) =>
        navBuildBitmap?.InvokeFunc(startingPos, filename, pixelSize) ?? false;

    /// <summary>
    ///     在限定范围内构建导航网格的位图表示
    /// </summary>
    /// <param name="startingPos">起始位置</param>
    /// <param name="filename">文件名</param>
    /// <param name="pixelSize">像素大小</param>
    /// <param name="minBounds">最小边界</param>
    /// <param name="maxBounds">最大边界</param>
    /// <returns></returns>
    public static bool NavBuildBitmapBounded(Vector3 startingPos, string filename, float pixelSize, Vector3 minBounds, Vector3 maxBounds) =>
        navBuildBitmapBounded?.InvokeFunc(startingPos, filename, pixelSize, minBounds, maxBounds) ?? false;

    /// <summary>
    ///     检查是否启用了自动加载
    /// </summary>
    /// <returns></returns>
    public static bool NavIsAutoLoad() =>
        navIsAutoLoad ?? false;

    /// <summary>
    ///     设置是否启用自动加载
    /// </summary>
    /// <param name="value">值</param>
    public static void NavSetAutoLoad(bool value) =>
        navSetAutoLoad?.InvokeAction(value);

    /// <summary>
    ///     检查寻路是否正在进行中
    /// </summary>
    /// <returns></returns>
    public static bool NavPathfindInProgress() =>
        navPathfindInProgress ?? false;

    /// <summary>
    ///     查询网格上最近的点
    /// </summary>
    /// <param name="pos">位置</param>
    /// <param name="halfExtentXZ">XZ半区</param>
    /// <param name="halfExtentY">Y半区</param>
    /// <returns></returns>
    public static Vector3? QueryMeshNearestPoint(Vector3 pos, float halfExtentXZ, float halfExtentY) =>
        queryMeshNearestPoint?.InvokeFunc(pos, halfExtentXZ, halfExtentY);

    /// <summary>
    ///     查询地板上的点
    /// </summary>
    /// <param name="pos">位置</param>
    /// <param name="allowUnlandable">允许无法降落</param>
    /// <param name="halfExtentXZ">XZ半区</param>
    /// <returns></returns>
    public static Vector3? QueryMeshPointOnFloor(Vector3 pos, bool allowUnlandable, float halfExtentXZ) =>
        queryMeshPointOnFloor?.InvokeFunc(pos, allowUnlandable, halfExtentXZ);

    /// <summary>
    ///     移动到路径点
    /// </summary>
    /// <param name="waypoints">路径点</param>
    /// <param name="fly">是否飞行</param>
    public static void PathMoveTo(List<Vector3> waypoints, bool fly) =>
        pathMoveTo?.InvokeAction(waypoints, fly);

    /// <summary>
    ///     停止移动
    /// </summary>
    public static void PathStop() =>
        pathStop?.InvokeAction();

    /// <summary>
    ///     检查是否正在移动
    /// </summary>
    /// <returns></returns>
    public static bool PathIsRunning() =>
        pathIsRunning ?? false;

    /// <summary>
    ///     获取路径点的数量
    /// </summary>
    /// <returns></returns>
    public static int PathNumWaypoints() =>
        pathNumWaypoints ?? 0;

    /// <summary>
    ///     获取路径点列表
    /// </summary>
    /// <returns></returns>
    public static List<Vector3> PathListWaypoints() =>
        pathListWaypoints?.InvokeFunc() ?? [];

    /// <summary>
    ///     获取是否允许移动
    /// </summary>
    /// <returns></returns>
    public static bool PathGetMovementAllowed() =>
        pathGetMovementAllowed ?? false;

    /// <summary>
    ///     设置是否允许移动
    /// </summary>
    /// <param name="value">值</param>
    public static void PathSetMovementAllowed(bool value) =>
        pathSetMovementAllowed?.InvokeAction(value);

    /// <summary>
    ///     获取是否对齐镜头
    /// </summary>
    /// <returns></returns>
    public static bool PathGetAlignCamera() =>
        pathGetAlignCamera ?? false;

    /// <summary>
    ///     设置是否对齐镜头
    /// </summary>
    /// <param name="value">值</param>
    public static void PathSetAlignCamera(bool value) =>
        pathSetAlignCamera?.InvokeAction(value);

    /// <summary>
    ///     获取容差
    /// </summary>
    /// <returns></returns>
    public static float PathGetTolerance() =>
        pathGetTolerance ?? 0f;

    /// <summary>
    ///     设置容差
    /// </summary>
    /// <param name="tolerance">容差值</param>
    public static void PathSetTolerance(float tolerance) =>
        pathSetTolerance?.InvokeAction(tolerance);

    /// <summary>
    ///     寻路并移动到目标点
    /// </summary>
    /// <param name="pos">目标点</param>
    /// <param name="fly">是否飞行</param>
    /// <returns></returns>
    public static bool PathfindAndMoveTo(Vector3 pos, bool fly) =>
        pathfindAndMoveTo?.InvokeFunc(pos, fly) ?? false;

    /// <summary>
    ///     检查寻路是否正在进行中
    /// </summary>
    /// <returns></returns>
    public static bool PathfindInProgress() =>
        pathfindInProgress ?? false;

    /// <summary>
    ///     取消所有查询
    /// </summary>
    public static void CancelAllQueries() =>
        pathfindCancelAll?.InvokeAction();

    /// <summary>
    ///     检查窗口是否打开
    /// </summary>
    /// <returns></returns>
    public static bool WindowIsOpen() =>
        windowIsOpen ?? false;

    /// <summary>
    ///     设置窗口是否打开
    /// </summary>
    /// <param name="value">值</param>
    public static void WindowSetOpen(bool value) =>
        windowSetOpen?.InvokeAction(value);

    /// <summary>
    ///     检查DTR栏信息是否显示
    /// </summary>
    /// <returns></returns>
    public static bool DtrIsShown() =>
        dtrIsShown ?? false;

    /// <summary>
    ///     设置DTR栏信息是否显示
    /// </summary>
    /// <param name="value">值</param>
    public static void DtrSetShown(bool value) =>
        dtrSetShown?.InvokeAction(value);

    internal static void Init() =>
        IPCAttributeRegistry.RegStaticIPCs(typeof(vnavmeshIPC));

    internal static void Uninit() =>
        IPCAttributeRegistry.UnregStaticIPCs(typeof(vnavmeshIPC));
}
