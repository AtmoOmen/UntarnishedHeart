using Newtonsoft.Json;
using OmenTools.Interop.Game.Lumina;
using OmenTools.OmenService;
using UntarnishedHeart.Execution.Condition.Configuration;
using UntarnishedHeart.Execution.Condition.Enums;
using Achievement = Lumina.Excel.Sheets.Achievement;

namespace UntarnishedHeart.Execution.Condition;

[JsonObject(MemberSerialization.OptIn)]
[ConditionJsonType("AchievementCount", ConditionDetectType.AchievementCount)]
public sealed class AchievementCountCondition : RouteValueConditionBase
{
    public override ConditionDetectType Kind => ConditionDetectType.AchievementCount;

    [JsonProperty("AchievementId")]
    public uint AchievementID { get; set; }

    protected override int GetCurrentValue(in ConditionContext context) =>
        (int)(AchievementManager.Instance().TryGetAchievement(AchievementID, out var achievementInfo)
                  ? achievementInfo.Current
                  : 0);

    protected override bool EqualsExtraCore(RouteValueConditionBase other) =>
        other is AchievementCountCondition condition &&
        AchievementID == condition.AchievementID;

    protected override int GetExtraHashCode() => (int)AchievementID;

    protected override RouteValueConditionBase DeepCopyCore() =>
        new AchievementCountCondition
        {
            ComparisonType = ComparisonType,
            ExpectedValue  = ExpectedValue,
            AchievementID  = AchievementID
        };

    protected override void DrawExtraFields()
    {
        DrawLabel("成就 ID", KnownColor.LightSkyBlue.ToVector4());
        var achievementID = AchievementID;
        if (ImGui.InputUInt("###AchievementIdInput", ref achievementID))
            AchievementID = achievementID;

        if (AchievementID == 0 || !LuminaGetter.TryGetRow(AchievementID, out Achievement achievementRow))
            return;

        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        ImGui.TableNextColumn();
        ImGui.TextUnformatted($"({achievementRow.Name})");
        ImGuiOm.TooltipHover($"{achievementRow.Description}");
    }
}
