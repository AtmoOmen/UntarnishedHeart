using Newtonsoft.Json;
using OmenTools.OmenService;
using UntarnishedHeart.Execution.Condition.Configuration;
using UntarnishedHeart.Execution.Condition.Enums;
using UntarnishedHeart.Execution.Condition.Helpers;
using UntarnishedHeart.Execution.Preset.Enums;
using UntarnishedHeart.Execution.Preset.Helpers;
using UntarnishedHeart.Windows;

namespace UntarnishedHeart.Execution.Condition;

[JsonObject(MemberSerialization.OptIn)]
[ConditionJsonType("HasSpecificTarget", ConditionDetectType.HasSpecificTarget)]
public sealed class HasSpecificTargetCondition : ConditionBase
{
    public override ConditionDetectType Kind => ConditionDetectType.HasSpecificTarget;

    [JsonProperty("ComparisonType")]
    public PresenceComparisonType ComparisonType { get; set; } = PresenceComparisonType.Has;

    [JsonProperty("Selector")]
    public TargetSelector Selector { get; set; } = new() { Kind = TargetSelectorKind.ByObjectKindAndDataID };

    public override bool Evaluate(in ConditionContext context)
    {
        var target = TargetManager.Target;
        var matches = target != null &&
                      Selector.Kind switch
                      {
                          TargetSelectorKind.CurrentTarget => true,
                          TargetSelectorKind.ByEntityID    => target.EntityID == Selector.EntityID,
                          TargetSelectorKind.ByObjectKindAndDataID =>
                              target.ObjectKind == Selector.ObjectKind &&
                              target.DataID     == Selector.DataID     &&
                              (!Selector.RequireTargetable || target.IsTargetable),
                          _ => false
                      };

        return ComparisonType == PresenceComparisonType.Has ? matches : !matches;
    }

    protected override bool EqualsCore(ConditionBase other) =>
        other is HasSpecificTargetCondition condition &&
        ComparisonType == condition.ComparisonType    &&
        Selector.Equals(condition.Selector);

    protected override int GetCoreHashCode() => HashCode.Combine((int)ComparisonType, Selector);

    public override ConditionBase DeepCopy() =>
        CopyBasePropertiesTo
        (
            new HasSpecificTargetCondition
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

        ConditionDrawHelper.DrawTargetSelector(Selector, "HasSpecificTarget");
    }
}
