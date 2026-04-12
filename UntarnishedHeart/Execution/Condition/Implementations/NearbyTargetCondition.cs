using Newtonsoft.Json;
using UntarnishedHeart.Execution.Condition.Configuration;
using UntarnishedHeart.Execution.Condition.Enums;
using UntarnishedHeart.Execution.Condition.Helpers;
using UntarnishedHeart.Execution.Preset.Enums;
using UntarnishedHeart.Execution.Preset.Helpers;
using UntarnishedHeart.Windows;

namespace UntarnishedHeart.Execution.Condition;

[JsonObject(MemberSerialization.OptIn)]
[ConditionJsonType("NearbyTarget", ConditionDetectType.NearbyTarget)]
public sealed class NearbyTargetCondition : ConditionBase
{
    public override ConditionDetectType Kind => ConditionDetectType.NearbyTarget;

    [JsonProperty("ComparisonType")]
    public PresenceComparisonType ComparisonType { get; set; } = PresenceComparisonType.Has;

    [JsonProperty("Selector")]
    public TargetSelector Selector { get; set; } = new() { Kind = TargetSelectorKind.ByObjectKindAndDataID };

    public override bool Evaluate(in ConditionContext context)
    {
        var exists = ResolveSpecificTarget(Selector) != null;
        return ComparisonType == PresenceComparisonType.Has ? exists : !exists;
    }

    protected override bool EqualsCore(ConditionBase other) =>
        other is NearbyTargetCondition condition   &&
        ComparisonType == condition.ComparisonType &&
        Selector.Equals(condition.Selector);

    protected override int GetCoreHashCode() => HashCode.Combine((int)ComparisonType, Selector);

    public override ConditionBase DeepCopy() =>
        CopyBasePropertiesTo
        (
            new NearbyTargetCondition
            {
                ComparisonType = ComparisonType,
                Selector       = TargetSelector.Copy(Selector)
            }
        );

    protected override void DrawBody()
    {
        DrawLabel("比较类型", KnownColor.LightSkyBlue.ToVector4());
        var comparisonCandidates = Enum.GetValues<PresenceComparisonType>();
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

        ConditionDrawHelper.DrawTargetSelector(Selector, "Nearby");
    }
}
