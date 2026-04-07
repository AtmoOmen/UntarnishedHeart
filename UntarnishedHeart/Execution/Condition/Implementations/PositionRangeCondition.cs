using System.Numerics;
using Newtonsoft.Json;
using UntarnishedHeart.Execution.Condition.Configuration;
using UntarnishedHeart.Execution.Condition.Enums;
using UntarnishedHeart.Execution.Condition.Helpers;
using UntarnishedHeart.Execution.Models;

namespace UntarnishedHeart.Execution.Condition;

[JsonObject(MemberSerialization.OptIn)]
[ConditionJsonType("PositionRange", ConditionDetectType.PositionRange)]
public sealed class PositionRangeCondition : ConditionBase
{
    public override ConditionDetectType Kind => ConditionDetectType.PositionRange;

    [JsonProperty("ComparisonType")]
    public PresenceComparisonType ComparisonType { get; set; } = PresenceComparisonType.Has;

    [JsonProperty("Range")]
    public PositionRange Range { get; set; } = new();

    public override bool Evaluate(in ConditionContext context)
    {
        if (context.LocalPlayer is not { } localPlayer)
            return false;

        var isInside = Vector3.DistanceSquared(localPlayer.Position, Range.Center) <= Range.Radius * Range.Radius;
        return ComparisonType == PresenceComparisonType.Has ? isInside : !isInside;
    }

    protected override bool EqualsCore(ConditionBase other) =>
        other is PositionRangeCondition condition  &&
        ComparisonType == condition.ComparisonType &&
        Range.Equals(condition.Range);

    protected override int GetCoreHashCode() => HashCode.Combine((int)ComparisonType, Range);

    public override ConditionBase DeepCopy() =>
        CopyBasePropertiesTo
        (
            new PositionRangeCondition
            {
                ComparisonType = ComparisonType,
                Range          = PositionRange.Copy(Range)
            }
        );

    protected override void DrawBody()
    {
        DrawLabel("比较类型", KnownColor.LightSkyBlue.ToVector4());
        ComparisonType = DrawEnumCombo("###ComparisonTypeCombo", ComparisonType);

        ConditionDrawHelper.DrawPositionRange(Range);
    }
}
