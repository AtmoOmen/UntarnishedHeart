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

        changed |= DrawRunSection(dutyOptions);

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        changed |= DrawFinderSection(dutyOptions);

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

    private static bool DrawRunSection(DutyOptions dutyOptions)
    {
        var changed = false;

        ImGui.TextColored(KnownColor.LightSkyBlue.ToVector4(), "运行策略");
        ImGui.Spacing();

        var runTimes = dutyOptions.RunTimes;
        if (ImGui.InputInt("运行次数###DutyOptionsRunTimes", ref runTimes))
        {
            dutyOptions.RunTimes = runTimes;
            changed              = true;
        }

        using (var table = ImRaii.Table("DutyOptionsRunTimesTable", 2, ImGuiTableFlags.SizingStretchSame))
        {
            if (table)
            {
                ImGui.TableNextRow();
                
                ImGui.TableNextColumn();
                var leaderMode = dutyOptions.LeaderMode;
                if (ImGui.Checkbox("队长模式###DutyOptionsLeaderMode", ref leaderMode))
                {
                    dutyOptions.LeaderMode = leaderMode;
                    changed                = true;
                }
                ImGuiOm.TooltipHover("勾选后, 运行预设时会自动进入指定副本");

                ImGui.TableNextColumn();
                var autoRecommendGear = dutyOptions.AutoRecommendGear;
                if (ImGui.Checkbox("自动最强装备###DutyOptionsAutoRecommendGear", ref autoRecommendGear))
                {
                    dutyOptions.AutoRecommendGear = autoRecommendGear;
                    changed                       = true;
                }
                ImGuiOm.TooltipHover("勾选后, 在进入副本后, 会自动装备当前职业的最强装备");
            }
        }
        
        

        return changed;
    }

    private static bool DrawFinderSection(DutyOptions dutyOptions)
    {
        var changed = false;
        var option  = dutyOptions.ContentsFinderOption;

        ImGui.TextColored(KnownColor.LightSkyBlue.ToVector4(), "匹配选项");
        ImGui.Spacing();

        using (var table = ImRaii.Table("DutyFinderOptionsTable", 2, ImGuiTableFlags.SizingStretchSame))
        {
            if (table)
            {
                ImGui.TableNextRow();
                changed |= DrawFinderOptionCell(0, "解除限制", option.UnrestrictedParty, value => option.UnrestrictedParty = value);
                changed |= DrawFinderOptionCell(1, "等级同步", option.LevelSync, value => option.LevelSync = value);

                ImGui.TableNextRow();

                changed |= DrawFinderOptionCell(0, "最低品级", option.MinimalIL, value => option.MinimalIL = value);
                changed |= DrawFinderOptionCell(1, "超越之力无效化", option.SilenceEcho, value => option.SilenceEcho = value);

                ImGui.TableNextRow();

                changed |= DrawFinderOptionCell(0, "中途加入", option.Supply, value => option.Supply = value);

                ImGui.TableSetColumnIndex(1);
                ImGui.TextDisabled("单人进入多变迷宫需要解除限制");
            }
        }
        
        var lootRule = option.LootRules;
        using (var combo = ImRaii.Combo("战利品分配###DutyOptionsLootRule", LootRuleNames[lootRule]))
        {
            if (combo)
            {
                foreach (var (loot, name) in LootRuleNames)
                {
                    if (!ImGui.Selectable(name, loot == lootRule))
                        continue;

                    option.LootRules = loot;
                    changed          = true;
                }
            }
        }
        
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

        if (changed)
            dutyOptions.ContentsFinderOption = option;

        return changed;
    }

    private static bool DrawEntrySection(DutyOptions dutyOptions)
    {
        var changed = false;
        var option  = dutyOptions.ContentsFinderOption;

        ImGui.TextColored(KnownColor.LightSkyBlue.ToVector4(), "掉落与入口");
        ImGui.Spacing();

        var lootRule = option.LootRules;
        if (ImGui.BeginCombo("战利品分配###DutyOptionsLootRule", LootRuleNames[lootRule]))
        {
            foreach (var (loot, name) in LootRuleNames)
            {
                if (!ImGui.Selectable(name, loot == lootRule))
                    continue;

                option.LootRules = loot;
                changed          = true;
            }

            ImGui.EndCombo();
        }

        ImGui.SetNextItemWidth(220f * GlobalUIScale);
        if (ImGui.BeginCombo("副本入口###DutyOptionsContentEntryCombo", dutyOptions.ContentEntryType.GetDescription()))
        {
            foreach (var entryType in Enum.GetValues<ContentEntryType>())
            {
                if (!ImGui.Selectable(entryType.GetDescription(), entryType == dutyOptions.ContentEntryType))
                    continue;

                dutyOptions.ContentEntryType = entryType;
                changed                      = true;
            }

            ImGui.EndCombo();
        }

        if (changed)
            dutyOptions.ContentsFinderOption = option;

        return changed;
    }

    private static bool DrawFinderOptionCell(int columnIndex, string label, bool currentValue, Action<bool> assign)
    {
        ImGui.TableSetColumnIndex(columnIndex);

        var value = currentValue;
        var changed = ImGui.Checkbox($"{label}##{label}", ref value);
        if (changed)
            assign(value);

        return changed;
    }

    public static void DrawAndSaveToConfig()
    {
        var dutyOptions = CreateFromConfig();
        if (!Draw(dutyOptions))
            return;

        PluginConfig.Instance().LeaderMode           = dutyOptions.LeaderMode;
        PluginConfig.Instance().AutoRecommendGear    = dutyOptions.AutoRecommendGear;
        PluginConfig.Instance().RunTimes             = dutyOptions.RunTimes;
        PluginConfig.Instance().ContentEntryType     = dutyOptions.ContentEntryType;
        PluginConfig.Instance().ContentsFinderOption = dutyOptions.ContentsFinderOption.Clone();
        PluginConfig.Instance().Save();
    }
}
