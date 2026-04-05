using Dalamud.Interface.Utility;
using UntarnishedHeart.Execution.Enums;
using UntarnishedHeart.Execution.Route;
using UntarnishedHeart.Internal;
using ContentsFinder = FFXIVClientStructs.FFXIV.Client.Game.UI.ContentsFinder;

namespace UntarnishedHeart.Windows.Components;

internal static class DutyOptionsEditor
{
    private static readonly Dictionary<ContentsFinder.LootRule, string> LootRuleNames = new()
    {
        [ContentsFinder.LootRule.Normal]     = "通常",
        [ContentsFinder.LootRule.GreedOnly]  = "仅限贪婪",
        [ContentsFinder.LootRule.Lootmaster] = "队长分配"
    };

    public static bool Draw(DutyOptions dutyOptions)
    {
        var changed = false;

        var runTimes = dutyOptions.RunTimes;
        ImGui.SetNextItemWidth(200f * GlobalUIScale);

        if (ImGui.InputInt("运行次数###DutyOptionsRunTimes", ref runTimes))
        {
            dutyOptions.RunTimes = runTimes;
            changed              = true;
        }

        ImGuiOm.TooltipHover("输入 -1 表示无限运行");

        var leaderMode = dutyOptions.LeaderMode;

        if (ImGui.Checkbox("队长模式###DutyOptionsLeaderMode", ref leaderMode))
        {
            dutyOptions.LeaderMode = leaderMode;
            changed                = true;
        }

        ImGuiOm.TooltipHover("启用后, 副本结束时会自动尝试排入同一副本");
        ImGui.SameLine();

        var autoRecommendGear = dutyOptions.AutoRecommendGear;

        if (ImGui.Checkbox("自动最强###DutyOptionsAutoRecommendGear", ref autoRecommendGear))
        {
            dutyOptions.AutoRecommendGear = autoRecommendGear;
            changed                       = true;
        }

        ImGuiOm.TooltipHover("启用后, 进入副本时会尝试切换当前职业的推荐装备");

        using (ImRaii.Group())
        {
            var option = dutyOptions.ContentsFinderOption;

            changed |= DrawFinderOptionCheckbox("解除限制###DutyOptionsUnrestricted", option.UnrestrictedParty, value => option.UnrestrictedParty = value);
            ImGui.SameLine();
            changed |= DrawFinderOptionCheckbox("等级同步###DutyOptionsLevelSync", option.LevelSync, value => option.LevelSync = value);
            ImGui.SameLine();
            changed |= DrawFinderOptionCheckbox("最低品级###DutyOptionsMinimalIL", option.MinimalIL, value => option.MinimalIL = value);
            ImGui.SameLine();
            changed |= DrawFinderOptionCheckbox("超越之力无效化###DutyOptionsSilenceEcho", option.SilenceEcho, value => option.SilenceEcho = value);
            ImGui.SameLine();
            changed |= DrawFinderOptionCheckbox("中途加入###DutyOptionsSupply", option.Supply, value => option.Supply = value);

            var lootRule = option.LootRules;
            var isFirst  = true;

            foreach (var (loot, loc) in LootRuleNames)
            {
                if (!isFirst)
                    ImGui.SameLine();
                isFirst = false;

                if (ImGui.RadioButton($"{loc}##DutyOptions{loot}", loot == lootRule))
                {
                    option.LootRules = loot;
                    changed          = true;
                }
            }

            if (changed)
                dutyOptions.ContentsFinderOption = option;
        }

        ImGui.SetNextItemWidth(200f * GlobalUIScale);

        using (var combo = ImRaii.Combo("副本入口###DutyOptionsContentEntryCombo", dutyOptions.ContentEntryType.GetDescription()))
        {
            if (combo)
            {
                foreach (var entryType in Enum.GetValues<ContentEntryType>())
                {
                    if (!ImGui.Selectable(entryType.GetDescription(), entryType == dutyOptions.ContentEntryType))
                        continue;

                    dutyOptions.ContentEntryType = entryType;
                    changed                      = true;
                }
            }
        }

        ImGuiOm.TooltipHover("单人进入多变迷宫时, 需要勾选解除限制并选择一般副本");

        return changed;
    }

    public static DutyOptions CreateFromConfig()
    {
        var config = PluginConfig.Instance();
        return new DutyOptions
        {
            LeaderMode           = config.LeaderMode,
            AutoRecommendGear    = config.AutoRecommendGear,
            RunTimes             = config.RunTimes,
            ContentEntryType     = config.ContentEntryType,
            ContentsFinderOption = config.ContentsFinderOption.Clone()
        };
    }

    private static bool DrawFinderOptionCheckbox(string label, bool currentValue, Action<bool> assign)
    {
        var value = currentValue;
        if (!ImGui.Checkbox(label, ref value))
            return false;

        assign(value);
        return true;
    }
}
