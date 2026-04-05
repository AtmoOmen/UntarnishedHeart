using FFXIVClientStructs.FFXIV.Client.Game;

namespace UntarnishedHeart.Execution.Models;

public sealed class ActionReference : IEquatable<ActionReference>
{
    public ActionType ActionType { get; set; } = ActionType.Action;

    public uint ActionID { get; set; }

    public bool Equals(ActionReference? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;

        return ActionType == other.ActionType && ActionID == other.ActionID;
    }

    public override bool Equals(object? obj) => Equals(obj as ActionReference);

    public override int GetHashCode() => HashCode.Combine((int)ActionType, ActionID);

    public static ActionReference Copy(ActionReference source) =>
        new()
        {
            ActionType = source.ActionType,
            ActionID   = source.ActionID
        };
}
