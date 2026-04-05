using System.Numerics;
using Dalamud.Game.ClientState.Objects.Enums;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using Newtonsoft.Json;
using UntarnishedHeart.Execution.Condition;
using UntarnishedHeart.Execution.Enums;

namespace UntarnishedHeart.Execution.Preset;

[JsonConverter(typeof(PresetStepJsonConverter))]
public class PresetStep : IEquatable<PresetStep>
{
    private static readonly HashSet<ObjectKind> NearestInteractableKinds =
    [
        ObjectKind.EventNpc,
        ObjectKind.EventObj,
        ObjectKind.Treasure,
        ObjectKind.Aetheryte,
        ObjectKind.GatheringPoint
    ];

    /// <summary>
    ///     步骤名称
    /// </summary>
    public string Note { get; set; } = string.Empty;

    /// <summary>
    ///     若为忙碌状态, 则等待
    /// </summary>
    public bool StopWhenBusy { get; set; }

    /// <summary>
    ///     若为战斗状态, 则等待
    /// </summary>
    public bool StopInCombat { get; set; } = true;

    /// <summary>
    ///     若任一队友不在无法战斗状态, 则等待
    /// </summary>
    public bool StopWhenAnyAlive { get; set; }

    public Vector3                           Position                   { get; set; }
    public MoveType                          MoveType                   { get; set; } = MoveType.传送;
    public bool                              WaitForGetClose            { get; set; }
    public uint                              DataID                     { get; set; }
    public ObjectKind                        ObjectKind                 { get; set; } = ObjectKind.BattleNpc;
    public bool                              WaitForTargetSpawn         { get; set; }
    public bool                              WaitForTarget              { get; set; } = true;
    public bool                              TargetNeedTargetable       { get; set; } = true;
    public bool                              InteractWithTarget         { get; set; }
    public bool                              InteractNeedTargetAnything { get; set; } = true;
    public bool                              InteractWithNearestObject  { get; set; }
    public string              Commands     { get; set; } = string.Empty;
    public ConditionCollection Condition    { get; set; } = new();
    public uint                Delay        { get; set; } = 5000;
    public int                 JumpToIndex  { get; set; } = -1;

    public bool Equals(PresetStep? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;

        return Note   == other.Note                            &&
               DataID == other.DataID                          &&
               Position.Equals(other.Position)                 &&
               Delay.Equals(other.Delay)                       &&
               StopInCombat              == other.StopInCombat &&
               InteractWithNearestObject == other.InteractWithNearestObject;
    }

    public unsafe void TargetObject()
    {
        if (FindObject() is not { } obj) return;

        TargetSystem.Instance()->Target = obj.ToStruct();
    }

    public IGameObject? FindObject() =>
        DService.Instance().ObjectTable.FirstOrDefault
        (x => x.ObjectKind == ObjectKind &&
              x.DataID     == DataID     &&
              (!TargetNeedTargetable || x.IsTargetable)
        );

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
                       .Where(x => x.Distance <= 400 && x.DistanceVertical <= 4)
                       .OrderBy(x => x.Distance)
                       .ThenBy(x => x.DistanceVertical)
                       .Select(x => x.Object)
                       .FirstOrDefault();
    }

    public static unsafe void OpenObjectInteraction(IGameObject gameObject)
    {
        if (gameObject.ObjectKind is not ObjectKind.EventObj)
            return;

        var structObject = gameObject.ToStruct();
        if (structObject == null)
            return;

        TargetSystem.Instance()->OpenObjectInteraction(structObject);
    }

    public override string ToString() =>
        $"ExecutorPresetStep_{Note}_{DataID}_{Position}_{Delay}_{StopInCombat}_{InteractWithNearestObject}";

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;

        return Equals((PresetStep)obj);
    }

    public override int GetHashCode() =>
        HashCode.Combine(Note, DataID, Position, Delay, StopInCombat, InteractWithNearestObject);

    public static PresetStep Copy(PresetStep source) =>
        new()
        {
            Note                       = source.Note,
            StopInCombat               = source.StopInCombat,
            StopWhenBusy               = source.StopWhenBusy,
            StopWhenAnyAlive           = source.StopWhenAnyAlive,
            Position                   = source.Position,
            MoveType                   = source.MoveType,
            WaitForGetClose            = source.WaitForGetClose,
            DataID                     = source.DataID,
            ObjectKind                 = source.ObjectKind,
            WaitForTarget              = source.WaitForTarget,
            WaitForTargetSpawn         = source.WaitForTargetSpawn,
            TargetNeedTargetable       = source.TargetNeedTargetable,
            InteractWithTarget         = source.InteractWithTarget,
            InteractNeedTargetAnything = source.InteractNeedTargetAnything,
            InteractWithNearestObject  = source.InteractWithNearestObject,
            Commands                   = source.Commands,
            Condition                  = ConditionCollection.Copy(source.Condition),
            Delay                      = source.Delay,
            JumpToIndex                = source.JumpToIndex
        };
}
