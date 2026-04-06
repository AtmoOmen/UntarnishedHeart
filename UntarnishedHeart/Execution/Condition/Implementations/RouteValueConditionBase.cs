using UntarnishedHeart.Execution.Condition.Enums;

namespace UntarnishedHeart.Execution.Condition;

public abstract class RouteValueConditionBase : Condition
{
    public NumericComparisonType ComparisonType { get; set; } = NumericComparisonType.EqualTo;

    public int ExpectedValue { get; set; }

    public override bool Evaluate(in ConditionContext context) =>
        CompareNumeric(ComparisonType, GetCurrentValue(context), ExpectedValue);

    protected override bool EqualsCore(Condition other) =>
        other is RouteValueConditionBase condition &&
        ComparisonType == condition.ComparisonType &&
        ExpectedValue == condition.ExpectedValue &&
        EqualsExtraCore(condition);

    protected virtual bool EqualsExtraCore(RouteValueConditionBase other) => true;

    protected override int GetCoreHashCode() => HashCode.Combine((int)ComparisonType, ExpectedValue, GetExtraHashCode());

    protected virtual int GetExtraHashCode() => 0;

    public override Condition DeepCopy() => DeepCopyCore();

    protected override void DrawBody()
    {
        DrawLabel("比较类型", KnownColor.LightSkyBlue.ToVector4());
        ComparisonType = DrawEnumCombo("###ComparisonTypeCombo", ComparisonType);

        DrawExtraFields();

        DrawLabel("目标值", KnownColor.LightSkyBlue.ToVector4());
        var expectedValue = ExpectedValue;
        if (ImGui.InputInt("###ExpectedValueInput", ref expectedValue))
            ExpectedValue = expectedValue;
    }

    protected abstract int GetCurrentValue(in ConditionContext context);

    protected abstract RouteValueConditionBase DeepCopyCore();

    protected virtual void DrawExtraFields()
    {
    }
}
