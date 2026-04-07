using FFXIVClientStructs.FFXIV.Client.Game;
using Newtonsoft.Json;
using UntarnishedHeart.Execution.Condition.Configuration;
using UntarnishedHeart.Execution.Condition.Enums;
using UntarnishedHeart.Execution.Condition.Helpers;
using UntarnishedHeart.Execution.Models;

namespace UntarnishedHeart.Execution.Condition;

[JsonObject(MemberSerialization.OptIn)]
[ConditionJsonType("ActionUsable", ConditionDetectType.ActionUsable)]
public sealed class ActionUsableCondition : ConditionBase
{
    public override ConditionDetectType Kind => ConditionDetectType.ActionUsable;

    [JsonProperty("ComparisonType")]
    public PresenceComparisonType ComparisonType { get; set; } = PresenceComparisonType.Has;

    [JsonProperty("Action")]
    public ActionReference Action { get; set; } = new();

    public override unsafe bool Evaluate(in ConditionContext context)
    {
        var actionStatus = ActionManager.Instance()->GetActionStatus(Action.ActionType, Action.ActionID);
        var isUsable     = actionStatus == 0;
        return ComparisonType == PresenceComparisonType.Has ? isUsable : !isUsable;
    }

    protected override bool EqualsCore(ConditionBase other) =>
        other is ActionUsableCondition condition   &&
        ComparisonType == condition.ComparisonType &&
        Action.Equals(condition.Action);

    protected override int GetCoreHashCode() => HashCode.Combine((int)ComparisonType, Action);

    public override ConditionBase DeepCopy() =>
        CopyBasePropertiesTo
        (
            new ActionUsableCondition
            {
                ComparisonType = ComparisonType,
                Action         = ActionReference.Copy(Action)
            }
        );

    protected override void DrawBody()
    {
        DrawLabel("比较类型", KnownColor.LightSkyBlue.ToVector4());
        ComparisonType = DrawEnumCombo("###ComparisonTypeCombo", ComparisonType);

        ConditionDrawHelper.DrawActionReference(Action);
    }
}
