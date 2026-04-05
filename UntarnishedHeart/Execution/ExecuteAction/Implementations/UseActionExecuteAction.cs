using System.Numerics;
using UntarnishedHeart.Execution.Condition;
using UntarnishedHeart.Execution.ExecuteAction.Enums;
using UntarnishedHeart.Execution.Models;
using UntarnishedHeart.Execution.Preset.Helpers;

namespace UntarnishedHeart.Execution.ExecuteAction.Implementations;

public sealed class UseActionExecuteAction : ExecuteAction
{
    public ActionReference Action { get; set; } = new();

    public TargetSelector TargetSelector { get; set; } = new();

    public Vector3 Location { get; set; }

    public bool UseLocation { get; set; }

    public override ExecuteActionKind Kind => ExecuteActionKind.UseAction;

    protected override bool EqualsCore(ExecuteAction other) =>
        other is UseActionExecuteAction action       &&
        Action.Equals(action.Action)                 &&
        TargetSelector.Equals(action.TargetSelector) &&
        Location.Equals(action.Location)             &&
        UseLocation == action.UseLocation;

    protected override int GetCoreHashCode() => HashCode.Combine(Action, TargetSelector, Location, UseLocation);

    public override ExecuteAction DeepCopy() =>
        new UseActionExecuteAction
        {
            Action         = ActionReference.Copy(Action),
            TargetSelector = TargetSelector.Copy(TargetSelector),
            Location       = Location,
            UseLocation    = UseLocation,
            Condition      = ConditionCollection.Copy(Condition)
        };
}
