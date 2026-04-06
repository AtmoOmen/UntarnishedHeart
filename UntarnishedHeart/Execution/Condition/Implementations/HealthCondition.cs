using UntarnishedHeart.Execution.Condition.Enums;

namespace UntarnishedHeart.Execution.Condition;

public sealed class HealthCondition : ConditionBase
{
    public override ConditionDetectType Kind => ConditionDetectType.Health;

    public NumericComparisonType ComparisonType { get; set; } = NumericComparisonType.LessThan;

    public ConditionTargetType TargetType { get; set; } = ConditionTargetType.Target;

    public float Threshold { get; set; }

    public override bool Evaluate(in ConditionContext context)
    {
        var target = ResolveTarget(context, TargetType);
        if (target is null || target.MaxHp <= 0)
            return false;

        var healthPercent = (float)target.CurrentHp / target.MaxHp * 100f;
        return CompareNumeric(ComparisonType, healthPercent, Threshold);
    }

    protected override bool EqualsCore(ConditionBase other) =>
        other is HealthCondition condition                                    &&
        ComparisonType                            == condition.ComparisonType &&
        TargetType                                == condition.TargetType     &&
        Math.Abs(Threshold - condition.Threshold) <= EQUALITY_TOLERANCE;

    protected override int GetCoreHashCode() => HashCode.Combine((int)ComparisonType, (int)TargetType, Threshold);

    public override ConditionBase DeepCopy() =>
        new HealthCondition
        {
            ComparisonType = ComparisonType,
            TargetType     = TargetType,
            Threshold      = Threshold
        };

    protected override void DrawBody()
    {
        DrawLabel("比较类型", KnownColor.LightSkyBlue.ToVector4());
        ComparisonType = DrawEnumCombo("###ComparisonTypeCombo", ComparisonType);

        TargetType = DrawTargetType("###TargetTypeCombo", TargetType);

        DrawLabel("百分比", KnownColor.LightSkyBlue.ToVector4());
        var threshold = Threshold;
        if (ImGui.InputFloat("%###ValueInput", ref threshold))
            Threshold = threshold;
    }
}
