using Newtonsoft.Json;
using UntarnishedHeart.Execution.Condition.Configuration;
using UntarnishedHeart.Execution.Condition.Enums;
using UntarnishedHeart.Windows;

namespace UntarnishedHeart.Execution.Condition;

[JsonObject(MemberSerialization.OptIn)]
[ConditionJsonType("TargetTargetIsSelf", ConditionDetectType.TargetTargetIsSelf)]
public sealed class TargetTargetIsSelfCondition : ConditionBase
{
    public override ConditionDetectType Kind => ConditionDetectType.TargetTargetIsSelf;

    [JsonProperty("ComparisonType")]
    public PresenceComparisonType ComparisonType { get; set; } = PresenceComparisonType.Has;

    public override bool Evaluate(in ConditionContext context)
    {
        var isSelf = context.Target                != null &&
                     context.LocalPlayer           != null &&
                     context.Target.TargetObjectID == context.LocalPlayer.GameObjectID;

        return ComparisonType == PresenceComparisonType.Has ? isSelf : !isSelf;
    }

    protected override bool EqualsCore(ConditionBase other) =>
        other is TargetTargetIsSelfCondition condition &&
        ComparisonType == condition.ComparisonType;

    protected override int GetCoreHashCode() => (int)ComparisonType;

    public override ConditionBase DeepCopy() =>
        CopyBasePropertiesTo(new TargetTargetIsSelfCondition { ComparisonType = ComparisonType });

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
    }
}
