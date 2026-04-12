using Lumina.Excel.Sheets;
using Newtonsoft.Json;
using OmenTools.Interop.Game.Lumina;
using UntarnishedHeart.Execution.Condition.Configuration;
using UntarnishedHeart.Execution.Condition.Enums;
using UntarnishedHeart.Windows;

namespace UntarnishedHeart.Execution.Condition;

[JsonObject(MemberSerialization.OptIn)]
[ConditionJsonType("Status", ConditionDetectType.Status)]
public sealed class StatusCondition : ConditionBase
{
    public override ConditionDetectType Kind => ConditionDetectType.Status;

    [JsonProperty("ComparisonType")]
    public PresenceComparisonType ComparisonType { get; set; } = PresenceComparisonType.Has;

    [JsonProperty("TargetType")]
    public ConditionTargetType TargetType { get; set; } = ConditionTargetType.Target;

    [JsonProperty("StatusId")]
    public uint StatusID { get; set; }

    public override bool Evaluate(in ConditionContext context)
    {
        var target    = ResolveTarget(context, TargetType);
        var hasStatus = target?.StatusList.HasStatus(StatusID) == true;
        return ComparisonType == PresenceComparisonType.Has ? hasStatus : !hasStatus;
    }

    protected override bool EqualsCore(ConditionBase other) =>
        other is StatusCondition condition         &&
        ComparisonType == condition.ComparisonType &&
        TargetType     == condition.TargetType     &&
        StatusID       == condition.StatusID;

    protected override int GetCoreHashCode() => HashCode.Combine((int)ComparisonType, (int)TargetType, StatusID);

    public override ConditionBase DeepCopy() =>
        CopyBasePropertiesTo
        (
            new StatusCondition
            {
                ComparisonType = ComparisonType,
                TargetType     = TargetType,
                StatusID       = StatusID
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

        DrawLabel("状态 ID", KnownColor.LightSkyBlue.ToVector4());
        var statusID = StatusID;
        if (ImGui.InputUInt("###StatusIdInput", ref statusID))
            StatusID = statusID;

        if (LuminaGetter.TryGetRow(StatusID, out Status row))
        {
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.TableNextColumn();
            ImGui.TextUnformatted($"({row.Name})");
        }
    }
}
