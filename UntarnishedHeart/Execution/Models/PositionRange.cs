using System.Numerics;

namespace UntarnishedHeart.Execution.Models;

public sealed class PositionRange : IEquatable<PositionRange>
{
    public Vector3 Center { get; set; }

    public float Radius { get; set; } = 1f;

    public bool Equals(PositionRange? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;

        return Center.Equals(other.Center) && Math.Abs(Radius - other.Radius) <= 0.01f;
    }

    public override bool Equals(object? obj) => Equals(obj as PositionRange);

    public override int GetHashCode() => HashCode.Combine(Center, Radius);

    public static PositionRange Copy(PositionRange source) =>
        new()
        {
            Center = source.Center,
            Radius = source.Radius
        };
}
