using Newtonsoft.Json;
using OmenTools.Interop.Game.Helpers;
using UntarnishedHeart.Execution.Condition.Configuration;
using UntarnishedHeart.Execution.Condition.Enums;
using UntarnishedHeart.Windows;

namespace UntarnishedHeart.Execution.Condition;

[JsonObject(MemberSerialization.OptIn)]
[ConditionJsonType("AddonOpened", ConditionDetectType.AddonOpened)]
public sealed class AddonOpenedCondition : ConditionBase
{
    public override ConditionDetectType Kind => ConditionDetectType.AddonOpened;

    [JsonProperty("AddonName")]
    public string AddonName { get; set; } = string.Empty;

    [JsonProperty("ComparisonType")]
    public PresenceComparisonType ComparisonType { get; set; } = PresenceComparisonType.Has;

    [JsonProperty("RequireAddonReady")]
    public bool RequireAddonReady { get; set; }

    public override unsafe bool Evaluate(in ConditionContext context)
    {
        var isOpened = AddonHelper.TryGetByName(AddonName, out var addon) &&
                       (!RequireAddonReady || addon->IsAddonAndNodesReady());

        return ComparisonType == PresenceComparisonType.Has ? isOpened : !isOpened;
    }

    protected override bool EqualsCore(ConditionBase other) =>
        other is AddonOpenedCondition condition       &&
        AddonName         == condition.AddonName      &&
        ComparisonType    == condition.ComparisonType &&
        RequireAddonReady == condition.RequireAddonReady;

    protected override int GetCoreHashCode() => HashCode.Combine(AddonName, (int)ComparisonType, RequireAddonReady);

    public override ConditionBase DeepCopy() =>
        CopyBasePropertiesTo
        (
            new AddonOpenedCondition
            {
                AddonName         = AddonName,
                ComparisonType    = ComparisonType,
                RequireAddonReady = RequireAddonReady
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

        DrawLabel("界面名称", KnownColor.LightSkyBlue.ToVector4());
        var addonName = AddonName;
        if (ImGui.InputText("###AddonNameInput", ref addonName, 128))
            AddonName = addonName;

        DrawLabel("完全可用", KnownColor.LightSkyBlue.ToVector4());
        var requireAddonReady = RequireAddonReady;
        if (ImGui.Checkbox("###RequireAddonReady", ref requireAddonReady))
            RequireAddonReady = requireAddonReady;
    }
}
