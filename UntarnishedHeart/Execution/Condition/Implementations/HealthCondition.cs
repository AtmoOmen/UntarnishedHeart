using Newtonsoft.Json;
using UntarnishedHeart.Execution.Condition.Configuration;
using UntarnishedHeart.Execution.Condition.Enums;
using UntarnishedHeart.Windows;

namespace UntarnishedHeart.Execution.Condition;

[JsonObject(MemberSerialization.OptIn)]
[ConditionJsonType("Health", ConditionDetectType.Health)]
public sealed class HealthCondition : ConditionBase
{
    public override ConditionDetectType Kind => ConditionDetectType.Health;

    [JsonProperty("ComparisonType")]
    public NumericComparisonType ComparisonType { get; set; } = NumericComparisonType.LessThan;

    [JsonProperty("TargetType")]
    public ConditionTargetType TargetType { get; set; } = ConditionTargetType.Target;

    [JsonProperty("Threshold")]
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
        CopyBasePropertiesTo
        (
            new HealthCondition
            {
                ComparisonType = ComparisonType,
                TargetType     = TargetType,
                Threshold      = Threshold
            }
        );

    protected override void DrawBody()
    {
        DrawLabel("比较类型", KnownColor.LightSkyBlue.ToVector4());
        var comparisonCandidates = Enum.GetValues<NumericComparisonType>();
        using (var combo = ImRaii.Combo("###ComparisonTypeCombo", ComparisonType.GetDescription(), ImGuiComboFlags.HeightLargest))
        {
            if (combo)
                ImGui.CloseCurrentPopup();
        }

        if (ImGui.IsItemClicked())
        {
            CollectionSelectorWindow.OpenEnum
            (
                "选择比较类型",
                "暂无可选比较类型",
                ComparisonType,
                value => ComparisonType = value,
                comparisonCandidates
            );
        }

        DrawLabel("目标类型", KnownColor.LightSkyBlue.ToVector4());
        var targetTypeCandidates = Enum.GetValues<ConditionTargetType>();
        using (var combo = ImRaii.Combo("###TargetTypeCombo", TargetType.GetDescription(), ImGuiComboFlags.HeightLargest))
        {
            if (combo)
                ImGui.CloseCurrentPopup();
        }

        if (ImGui.IsItemClicked())
        {
            CollectionSelectorWindow.OpenEnum
            (
                "选择目标类型",
                "暂无可选目标类型",
                TargetType,
                value => TargetType = value,
                targetTypeCandidates
            );
        }

        DrawLabel("百分比", KnownColor.LightSkyBlue.ToVector4());
        var threshold = Threshold;
        if (ImGui.InputFloat("%###ValueInput", ref threshold))
            Threshold = threshold;
    }
}
