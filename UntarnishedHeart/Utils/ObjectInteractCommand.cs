using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.ClientState.Objects.Enums;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using FFXIVClientStructs.FFXIV.Client.Game.Event;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using Lumina.Excel.GeneratedSheets;
using ObjectKind = Dalamud.Game.ClientState.Objects.Enums.ObjectKind;



namespace DailyRoutines.Modules;

public unsafe class AutoObjectInteract
{
    private readonly HashSet<ObjectKind> _targetableKinds = new()
    {
        ObjectKind.EventNpc,
        ObjectKind.EventObj,
        ObjectKind.Treasure,
        ObjectKind.Aetheryte,
        ObjectKind.GatheringPoint,
    };
    private const string ENPCTiltleText = "[{0}] {1}";
    private static readonly Dictionary<uint, string> ENpcTitles;
    private static readonly HashSet<uint> ImportantENPC;
    private readonly TaskHelper _taskHelper;

    public AutoObjectInteract()
    {
        _taskHelper = new TaskHelper { TimeLimitMS = 5_000 };

    }
    static AutoObjectInteract()
    {
        ENpcTitles = LuminaCache.Get<ENpcResident>()
                                .Where(x => x.Unknown10 && !string.IsNullOrWhiteSpace(x.Title.ExtractText()))
                                .ToDictionary(x => x.RowId, x => x.Title.ExtractText());

        ImportantENPC = LuminaCache.Get<ENpcResident>()
                                        .Where(x => x.Unknown10)
                                        .Select(x => x.RowId)
                                        .ToHashSet();

    }
    public bool TryInteractNearestObject()
    {
        var localPlayer = DService.ClientState.LocalPlayer;
        if (localPlayer == null) return false;

        GameObject* nearestObject = null;
        float nearestDistance = float.MaxValue;
        ObjectKind nearestKind = ObjectKind.None;
        var objects = DService.ObjectTable.ToArray();
        // Find nearest valid object
        foreach (var obj in objects)
        {
            if (!obj.IsTargetable || obj.IsDead || !obj.IsValid()) continue;
            var objKind = obj.ObjectKind;
            var gameObj = obj.ToStruct();
            var dataID = obj.DataId;
            var objName = obj.Name.TextValue;
            if (gameObj == null) continue;
            if (!_targetableKinds.Contains(obj.ObjectKind)) continue;

            if (objKind == ObjectKind.EventNpc)
            {
                if (ImportantENPC.Contains(dataID))
                {
                    if (ENpcTitles.TryGetValue(dataID, out var ENPCTitle))
                        objName = string.Format(ENPCTiltleText, ENPCTitle, obj.Name);
                }
                else if (gameObj->NamePlateIconId == 0) continue;
            }

            var objDistance = Vector3.DistanceSquared(localPlayer.Position, obj.Position);
            if (objDistance > 400 || localPlayer.Position.Y - gameObj->Position.Y > 4) continue;

            if (objDistance < nearestDistance)
            {
                nearestDistance = objDistance;
                nearestObject = gameObj;
                nearestKind = obj.ObjectKind;
            }
        }

        // Interact with nearest object if found
        if (nearestObject != null)
        {
            InteractWithObject(nearestObject, nearestKind);
            return true;
        }

        return false;
    }

    private bool IsValidTarget(dynamic obj)
    {
        return obj.IsValid() &&
               obj.IsTargetable &&
               !obj.IsDead &&
               _targetableKinds.Contains(obj.ObjectKind);
    }

    private void InteractWithObject(GameObject* obj, ObjectKind kind)
    {
        _taskHelper.RemoveAllTasks(2);


        _taskHelper.Enqueue(() =>
        {
            if (IsOnMount()) return false;

            TargetSystem.Instance()->Target = obj;
            return TargetSystem.Instance()->InteractWithObject(obj) != 0;
        }, "Interact", null, null, 2);

        if (kind is ObjectKind.EventObj)
        {
            _taskHelper.Enqueue(
                () => TargetSystem.Instance()->OpenObjectInteraction(obj),
                "OpenInteraction", null, null, 2);
        }
    }

    private static bool IsOnMount()
    {
        return DService.Condition[ConditionFlag.Mounted];
    }
}
