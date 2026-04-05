using Dalamud.Game.ClientState.Objects.Enums;
using UntarnishedHeart.Execution.Preset.Enums;

namespace UntarnishedHeart.Execution.Preset.Helpers;

public sealed class TargetSelector : IEquatable<TargetSelector>
{
    public TargetSelectorKind Kind { get; set; }

    public ObjectKind ObjectKind { get; set; } = ObjectKind.BattleNpc;

    public uint DataID { get; set; }

    public uint EntityID { get; set; }

    public bool RequireTargetable { get; set; } = true;

    public bool Equals(TargetSelector? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;

        return Kind              == other.Kind       &&
               ObjectKind        == other.ObjectKind &&
               DataID            == other.DataID     &&
               EntityID          == other.EntityID   &&
               RequireTargetable == other.RequireTargetable;
    }

    public override bool Equals(object? obj) => Equals(obj as TargetSelector);

    public override int GetHashCode() => HashCode.Combine((int)Kind, (int)ObjectKind, DataID, EntityID, RequireTargetable);

    public static TargetSelector Copy(TargetSelector source) =>
        new()
        {
            Kind              = source.Kind,
            ObjectKind        = source.ObjectKind,
            DataID            = source.DataID,
            EntityID          = source.EntityID,
            RequireTargetable = source.RequireTargetable
        };
}
