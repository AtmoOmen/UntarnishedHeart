using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Game.ClientState.Conditions;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using ObjectKind = Dalamud.Game.ClientState.Objects.Enums.ObjectKind;


namespace UntarnishedHeart.Utils;

public static unsafe class AutoObjectInteract
{
    private static readonly HashSet<ObjectKind> ValidInteractableKinds =
    [
        ObjectKind.EventNpc,
        ObjectKind.EventObj,
        ObjectKind.Treasure,
        ObjectKind.Aetheryte,
        ObjectKind.GatheringPoint
    ];

    private static readonly TaskHelper TaskHelper = new() { TimeLimitMS = 5_000 };

    public static void TryInteractNearestObject()
    {
        if (DService.ObjectTable.LocalPlayer is not { } localPlayer) return;

        var nearestObj = DService.ObjectTable.Where(x => x is { IsTargetable: true, IsDead: false }    &&
                                                         ValidInteractableKinds.Contains(x.ObjectKind) && x.IsValid())
                                 .Select(x => new
                                 {
                                     Object           = x,
                                     Distance         = Vector3.DistanceSquared(localPlayer.Position, x.Position),
                                     DistanceVertical = Math.Abs(localPlayer.Position.Y - x.Position.Y)
                                 })
                                 .Where(x => x.Distance <= 400 && x.DistanceVertical <= 4)
                                 .OrderBy(x => x.Distance)
                                 .ThenBy(x => x.DistanceVertical)
                                 .FirstOrDefault();

        if (nearestObj == null) return;
        
        var gameObj = nearestObj.Object.ToStruct();
        if (gameObj == null) return;
        
        TaskHelper.Abort();
        TaskHelper.Enqueue(() =>
        {
            if (IsOnMount() || OccupiedInEvent) return false;

            return nearestObj.Object.TargetInteract();
        }, "Interact");

        if (nearestObj.Object.ObjectKind is ObjectKind.EventObj)
            TaskHelper.Enqueue(() => TargetSystem.Instance()->OpenObjectInteraction(gameObj), "OpenInteraction");
    }

    private static bool IsOnMount() 
        => DService.Condition[ConditionFlag.Mounted] || DService.Condition[ConditionFlag.Mounted2];
}
