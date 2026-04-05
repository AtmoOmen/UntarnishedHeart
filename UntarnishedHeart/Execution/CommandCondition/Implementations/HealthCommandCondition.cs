using UntarnishedHeart.Execution.CommandCondition.Enums;

namespace UntarnishedHeart.Execution.CommandCondition;

public sealed class HealthCommandCondition : CommandSingleCondition
{
    public override CommandDetectType Kind => CommandDetectType.Health;

    public NumericComparisonType ComparisonType { get; set; } = NumericComparisonType.LessThan;

    public CommandTargetType TargetType { get; set; } = CommandTargetType.Target;

    public float Threshold { get; set; }

    public override bool Evaluate(in CommandConditionContext context)
    {
        var target = ResolveTarget(context, TargetType);
        if (target is null || target.MaxHp <= 0)
            return false;

        var healthPercent = (float)target.CurrentHp / target.MaxHp * 100f;
        return ComparisonType switch
        {
            NumericComparisonType.GreaterThan => healthPercent                        > Threshold,
            NumericComparisonType.LessThan    => healthPercent                        < Threshold,
            NumericComparisonType.EqualTo     => MathF.Abs(healthPercent - Threshold) <= EQUALITY_TOLERANCE,
            NumericComparisonType.NotEqualTo  => MathF.Abs(healthPercent - Threshold) > EQUALITY_TOLERANCE,
            _                                 => false
        };
    }

    public override CommandSingleCondition DeepCopy() =>
        new HealthCommandCondition
        {
            ComparisonType = ComparisonType,
            TargetType     = TargetType,
            Threshold      = Threshold
        };

    protected override void DrawBody()
    {
        DrawLabel("比较类型", KnownColor.LightSkyBlue.ToVector4());
        ComparisonType = DrawEnumCombo("###ComparisonTypeCombo", ComparisonType);

        DrawLabel("目标类型", KnownColor.LightSkyBlue.ToVector4());
        TargetType = DrawEnumCombo("###TargetTypeCombo", TargetType);

        DrawLabel("值", KnownColor.LightSkyBlue.ToVector4());
        var threshold = Threshold;
        if (ImGui.InputFloat("%###ValueInput", ref threshold))
            Threshold = threshold;
    }

    protected override string Describe() =>
        $"生命值百分比 {TargetType.GetDescription()} {ComparisonType.GetDescription()} {Threshold}";
}
