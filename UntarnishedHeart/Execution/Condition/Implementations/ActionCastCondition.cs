using Newtonsoft.Json;
using UntarnishedHeart.Execution.Condition.Configuration;
using UntarnishedHeart.Execution.Condition.Enums;
using UntarnishedHeart.Execution.Condition.Helpers;
using UntarnishedHeart.Execution.Models;
using UntarnishedHeart.Windows;

namespace UntarnishedHeart.Execution.Condition;

[JsonObject(MemberSerialization.OptIn)]
[ConditionJsonType("ActionCast", ConditionDetectType.ActionCast)]
public sealed class ActionCastCondition : ConditionBase
{
    public override ConditionDetectType Kind => ConditionDetectType.ActionCast;

    [JsonProperty("ComparisonType")]
    public PresenceComparisonType ComparisonType { get; set; } = PresenceComparisonType.Has;

    [JsonProperty("TargetType")]
    public ConditionTargetType TargetType { get; set; } = ConditionTargetType.Target;

    [JsonProperty("Action")]
    public ActionReference Action { get; set; } = new();

    public override bool Evaluate(in ConditionContext context)
    {
        var target = ResolveTarget(context, TargetType);
        var isCasting = target is { IsCasting: true }              &&
                        target.CastActionType == Action.ActionType &&
                        target.CastActionID   == Action.ActionID;

        return ComparisonType == PresenceComparisonType.Has ? isCasting : !isCasting;
    }

    protected override bool EqualsCore(ConditionBase other) =>
        other is ActionCastCondition condition     &&
        ComparisonType == condition.ComparisonType &&
        TargetType     == condition.TargetType     &&
        Action.Equals(condition.Action);

    protected override int GetCoreHashCode() => HashCode.Combine((int)ComparisonType, (int)TargetType, Action);

    public override ConditionBase DeepCopy() =>
        CopyBasePropertiesTo
        (
            new ActionCastCondition
            {
                ComparisonType = ComparisonType,
                TargetType     = TargetType,
                Action         = ActionReference.Copy(Action)
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

        ConditionDrawHelper.DrawActionReference(Action);
    }
}
