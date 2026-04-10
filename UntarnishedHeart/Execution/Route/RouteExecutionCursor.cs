using UntarnishedHeart.Execution.Common;

namespace UntarnishedHeart.Execution.Route;

public sealed class RouteExecutionCursor
{
    public required ExecuteActionRuntimeCursor RouteCursor { get; init; }

    public required ExecuteActionRuntimeCursor? PresetCursor { get; init; }
}
