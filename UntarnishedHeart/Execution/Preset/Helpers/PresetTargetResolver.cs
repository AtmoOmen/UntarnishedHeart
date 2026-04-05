using System.Numerics;
using Dalamud.Game.ClientState.Objects.Enums;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using OmenTools.OmenService;
using UntarnishedHeart.Execution.Preset.Enums;

namespace UntarnishedHeart.Execution.Preset.Helpers;

internal static class PresetTargetResolver
{
    private static readonly HashSet<ObjectKind> NearestInteractableKinds =
    [
        ObjectKind.EventNpc,
        ObjectKind.EventObj,
        ObjectKind.Treasure,
        ObjectKind.Aetheryte,
        ObjectKind.GatheringPoint
    ];

    public static IGameObject? Resolve(TargetSelector selector)
    {
        ArgumentNullException.ThrowIfNull(selector);

        return selector.Kind switch
        {
            TargetSelectorKind.CurrentTarget => TargetManager.Target,
            TargetSelectorKind.ByEntityID => selector.EntityID == 0
                                                 ? null
                                                 : DService.Instance().ObjectTable.SearchByEntityID(selector.EntityID),
            TargetSelectorKind.ByObjectKindAndDataID => FindNearest(selector),
            _                                        => null
        };
    }

    public static IGameObject? FindNearest(TargetSelector selector)
    {
        if (selector.DataID == 0)
            return null;

        var candidates = DService.Instance().ObjectTable
                                 .Where
                                 (x => x.ObjectKind == selector.ObjectKind &&
                                       x.DataID     == selector.DataID     &&
                                       (!selector.RequireTargetable || x.IsTargetable)
                                 );

        if (DService.Instance().ObjectTable.LocalPlayer is not { } localPlayer)
            return candidates.FirstOrDefault();

        return candidates.MinBy(x => Vector3.DistanceSquared(localPlayer.Position, x.Position));
    }

    public static IGameObject? FindNearestInteractableObject()
    {
        if (DService.Instance().ObjectTable.LocalPlayer is not { } localPlayer)
            return null;

        return DService.Instance().ObjectTable
                       .Where(x => x is { IsTargetable: true, IsDead: false } && NearestInteractableKinds.Contains(x.ObjectKind) && x.IsValid())
                       .Select
                       (x => new
                           {
                               Object           = x,
                               Distance         = Vector3.DistanceSquared(localPlayer.Position, x.Position),
                               DistanceVertical = Math.Abs(localPlayer.Position.Y - x.Position.Y)
                           }
                       )
                       .Where(x => x.Distance <= 400f && x.DistanceVertical <= 4f)
                       .OrderBy(x => x.Distance)
                       .ThenBy(x => x.DistanceVertical)
                       .Select(x => x.Object)
                       .FirstOrDefault();
    }

    public static unsafe void SelectTarget(IGameObject? gameObject)
    {
        if (gameObject == null)
            return;

        TargetSystem.Instance()->Target = gameObject.ToStruct();
    }

    public static unsafe void OpenObjectInteraction(IGameObject? gameObject)
    {
        if (gameObject is not { ObjectKind: ObjectKind.EventObj })
            return;

        var structObject = gameObject.ToStruct();
        if (structObject == null)
            return;

        TargetSystem.Instance()->OpenObjectInteraction(structObject);
    }
}
