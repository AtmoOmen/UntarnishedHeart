using Dalamud.Game.ClientState.Conditions;
using UntarnishedHeart.Execution.Condition.Enums;

namespace UntarnishedHeart.Execution.Condition;

public sealed class GameConditionStateCondition : ConditionBase
{
    public override ConditionDetectType Kind => ConditionDetectType.GameCondition;

    public ConditionFlag Flag { get; set; } = ConditionFlag.InCombat;

    public PresenceComparisonType ComparisonType { get; set; } = PresenceComparisonType.NotHas;

    public override bool Evaluate(in ConditionContext context)
    {
        var hasFlag = DService.Instance().Condition[Flag];
        return ComparisonType == PresenceComparisonType.Has ? hasFlag : !hasFlag;
    }

    protected override bool EqualsCore(ConditionBase other) =>
        other is GameConditionStateCondition condition &&
        Flag           == condition.Flag               &&
        ComparisonType == condition.ComparisonType;

    protected override int GetCoreHashCode() => HashCode.Combine((int)Flag, (int)ComparisonType);

    public override ConditionBase DeepCopy() =>
        CopyBasePropertiesTo
        (
            new GameConditionStateCondition
            {
                Flag           = Flag,
                ComparisonType = ComparisonType
            }
        );

    protected override void DrawBody()
    {
        DrawLabel("比较类型", KnownColor.LightSkyBlue.ToVector4());
        ComparisonType = DrawEnumCombo("###ComparisonTypeCombo", ComparisonType);

        DrawLabel("ConditionFlag", KnownColor.LightSkyBlue.ToVector4());
        Flag = DrawEnumCombo("###ConditionFlagCombo", Flag);
    }
}
