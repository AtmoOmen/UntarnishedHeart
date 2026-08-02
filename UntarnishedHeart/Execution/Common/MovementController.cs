using System.Numerics;
using OmenTools.Dalamud;
using OmenTools.Interop.Game;
using OmenTools.Interop.Windows.Helpers;
using OmenTools.OmenService;

namespace UntarnishedHeart.Execution.Common;

internal sealed class MovementController : IDisposable
{
    private CancellationTokenSource? movementCancellationSource;
    private Task?                    movementTask;

    public void Cancel()
    {
        if (movementCancellationSource is not { IsCancellationRequested: false } movementCts)
            return;

        movementCts.Cancel();
    }

    public void Dispose()
    {
        Cancel();

        movementCancellationSource?.Dispose();
        movementCancellationSource = null;
        movementTask               = null;
    }

    public static unsafe void Teleport(Vector3 position)
    {
        if (DService.Instance().ObjectTable.LocalPlayer is not { } localPlayer)
            return;

        localPlayer.ToStruct()->SetPosition(position.X, position.Y, position.Z);
        KeyEmulationHelper.SendKeypress(Keys.W);
    }

    public void StartPathfindMovement(Vector3 position, CancellationToken parentToken) =>
        Start
        (
            async token =>
            {
                using var movementController = new MovementInputController();
                movementController.DesiredPosition = position;
                movementController.Enabled         = true;

                try
                {
                    while (!token.IsCancellationRequested)
                    {
                        if (DService.Instance().ObjectTable.LocalPlayer is not { } localPlayer)
                        {
                            await Task.Delay(100, token);
                            continue;
                        }

                        if (Vector3.DistanceSquared(localPlayer.Position, position) <= 2f)
                            break;

                        await Task.Delay(500, token);
                    }
                }
                finally
                {
                    movementController.Enabled         = false;
                    movementController.DesiredPosition = default;
                }
            },
            parentToken
        );

    public void StartVnavmeshMovement(Vector3 position, CancellationToken parentToken) =>
        Start(token => RunVnavmeshMovementAsync(position, false, token), parentToken);

    private void Start(Func<CancellationToken, Task> workFactory, CancellationToken parentToken)
    {
        Cancel();

        var movementCts = CancellationTokenSource.CreateLinkedTokenSource(parentToken);
        movementCancellationSource = movementCts;

        movementTask = DService.Instance().Framework.Run
        (
            async () =>
            {
                try
                {
                    await workFactory(movementCts.Token);
                }
                catch (OperationCanceledException) when (movementCts.IsCancellationRequested)
                {
                }
                catch (Exception ex)
                {
                    NotifyHelper.Instance().Chat($"移动执行失败: {ex.Message}");
                }
                finally
                {
                    if (ReferenceEquals(movementCancellationSource, movementCts))
                    {
                        movementCancellationSource = null;
                        movementTask               = null;
                    }

                    movementCts.Dispose();
                }
            },
            movementCts.Token
        );
    }

    private async Task RunVnavmeshMovementAsync(Vector3 position, bool fly, CancellationToken cancellationToken)
    {
        try
        {
            var timeout = DateTime.Now.AddSeconds(10);
            while (!vnavmeshIPC.GetIsNavReady() && DateTime.Now < timeout)
                await Task.Delay(100, cancellationToken);

            if (!vnavmeshIPC.GetIsNavReady())
            {
                NotifyHelper.Instance().ChatError("vnavmesh 未准备就绪");
                return;
            }

            if (!vnavmeshIPC.PathfindAndMoveTo(position, fly))
            {
                NotifyHelper.Instance().ChatError("vnavmesh 寻路启动失败");
                return;
            }

            await Task.Delay(500, cancellationToken);

            while (!cancellationToken.IsCancellationRequested)
            {
                if (DService.Instance().ObjectTable.LocalPlayer is not { } localPlayer)
                {
                    await Task.Delay(100, cancellationToken);
                    continue;
                }

                var distance = Vector3.Distance(localPlayer.Position, position);
                if (distance <= 2f)
                    break;

                if (!vnavmeshIPC.GetIsPathfindRunning() && !vnavmeshIPC.GetIsNavPathfindInProgress())
                {
                    await Task.Delay(500, cancellationToken);
                    distance = Vector3.Distance(localPlayer.Position, position);

                    if (distance > 2f)
                        NotifyHelper.Instance().Chat($"vnavmesh 寻路结束但未到达目标，距离: {distance:F2} 米");

                    break;
                }

                await Task.Delay(100, cancellationToken);
            }
        }
        finally
        {
            vnavmeshIPC.StopPathfind();
        }
    }
}
