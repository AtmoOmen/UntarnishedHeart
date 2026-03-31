namespace UntarnishedHeart.Execution.Route.Enums;

/// <summary>
///     路线执行状态
/// </summary>
public enum RouteExecutorState
{
    /// <summary>
    ///     未开始
    /// </summary>
    NotStarted,

    /// <summary>
    ///     运行中
    /// </summary>
    Running,

    /// <summary>
    ///     等待执行器完成
    /// </summary>
    WaitingForExecutor,

    /// <summary>
    ///     已完成
    /// </summary>
    Completed,

    /// <summary>
    ///     已停止
    /// </summary>
    Stopped,

    /// <summary>
    ///     出错
    /// </summary>
    Error
}
