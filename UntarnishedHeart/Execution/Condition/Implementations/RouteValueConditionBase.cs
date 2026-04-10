using Newtonsoft.Json;
using UntarnishedHeart.Execution.Condition.Enums;
using UntarnishedHeart.Windows;

namespace UntarnishedHeart.Execution.Condition;

[JsonObject(MemberSerialization.OptIn)]
public abstract class RouteValueConditionBase : ConditionBase
{
    [JsonProperty("ComparisonType")]
    public NumericComparisonType ComparisonType { get; set; } = NumericComparisonType.EqualTo;

    [JsonProperty("ExpectedValue")]
    public int ExpectedValue { get; set; }

    public override bool Evaluate(in ConditionContext context) =>
        CompareNumeric(ComparisonType, GetCurrentValue(context), ExpectedValue);

    protected override bool EqualsCore(ConditionBase other) =>
        other is RouteValueConditionBase condition &&
        ComparisonType == condition.ComparisonType &&
        ExpectedValue  == condition.ExpectedValue  &&
        EqualsExtraCore(condition);

    protected virtual bool EqualsExtraCore(RouteValueConditionBase other) => true;

    protected override int GetCoreHashCode() => HashCode.Combine((int)ComparisonType, ExpectedValue, GetExtraHashCode());

    protected virtual int GetExtraHashCode() => 0;

    public override ConditionBase DeepCopy() => CopyBasePropertiesTo(DeepCopyCore());

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
            var request = new CollectionSelectorRequest
            (
                "选择比较类型",
                "暂无可选比较类型",
                Array.IndexOf(comparisonCandidates, ComparisonType),
                comparisonCandidates.Select(candidate => new CollectionSelectorItem(candidate.GetDescription())).ToArray()
            );

            CollectionSelectorWindow.Open
            (
                request,
                index =>
                {
                    if ((uint)index >= (uint)comparisonCandidates.Length)
                        return;

                    ComparisonType = comparisonCandidates[index];
                }
            );
        }

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
