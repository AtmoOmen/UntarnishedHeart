using UntarnishedHeart.Execution.Enums;
using UntarnishedHeart.Execution.Route;
using UntarnishedHeart.Internal;
using UntarnishedHeart.Execution.Condition;
using UntarnishedHeart.Windows;
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

        ImGui.TextColored(KnownColor.LightSkyBlue.ToUInt(), "运行策略");
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

        ImGui.TextColored(KnownColor.LightSkyBlue.ToUInt(), "匹配选项");
        ImGui.Spacing();

        using (var table = ImRaii.Table("DutyFinderOptionsTable", 2, ImGuiTableFlags.SizingStretchSame))
        {
            if (table)
            {
                ImGui.TableNextRow();
                changed |= DrawFinderOptionCell(0, "解除限制", option.UnrestrictedParty, value => option.UnrestrictedParty = value);
                changed |= DrawFinderOptionCell(1, "等级同步", option.LevelSync,         value => option.LevelSync         = value);

                ImGui.TableNextRow();

                changed |= DrawFinderOptionCell(0, "最低品级",    option.MinimalIL,   value => option.MinimalIL   = value);
                changed |= DrawFinderOptionCell(1, "超越之力无效化", option.SilenceEcho, value => option.SilenceEcho = value);

                ImGui.TableNextRow();

                changed |= DrawFinderOptionCell(0, "中途加入", option.Supply, value => option.Supply = value);

                ImGui.TableSetColumnIndex(1);
                ImGui.TextDisabled("单人进入多变迷宫需要解除限制");
            }
        }

        var lootRule = option.LootRules;

        DrawLootRuleSelector(lootRule, loot =>
        {
            option.LootRules = loot;
            changed          = true;
        });

        ConditionBase.DrawEnumLocalizedSelector
        (
            "副本入口###DutyOptionsContentEntryCombo",
            "选择副本入口",
            "暂无可选副本入口",
            dutyOptions.ContentEntryType,
            entryType =>
            {
                dutyOptions.ContentEntryType = entryType;
                changed                      = true;
            },
            static value => value.GetDescription()
        );

        if (changed)
            dutyOptions.ContentsFinderOption = option;

        return changed;
    }

    private static bool DrawEntrySection(DutyOptions dutyOptions)
    {
        var changed = false;
        var option  = dutyOptions.ContentsFinderOption;

        ImGui.TextColored(KnownColor.LightSkyBlue.ToUInt(), "掉落与入口");
        ImGui.Spacing();

        var lootRule = option.LootRules;

        DrawLootRuleSelector(lootRule, loot =>
        {
            option.LootRules = loot;
            changed          = true;
        });

        ImGui.SetNextItemWidth(220f * GlobalUIScale);

        ConditionBase.DrawEnumLocalizedSelector
        (
            "副本入口###DutyOptionsContentEntryCombo",
            "选择副本入口",
            "暂无可选副本入口",
            dutyOptions.ContentEntryType,
            entryType =>
            {
                dutyOptions.ContentEntryType = entryType;
                changed                      = true;
            },
            static value => value.GetDescription()
        );

        if (changed)
            dutyOptions.ContentsFinderOption = option;

        return changed;
    }

    private static void DrawLootRuleSelector(ContentsFinder.LootRule current, Action<ContentsFinder.LootRule> setCurrent)
    {
        ImGui.SetNextItemWidth(220f * GlobalUIScale);

        using var combo = ImRaii.Combo("战利品分配###DutyOptionsLootRule", LootRuleNames[current]);
        if (combo)
            ImGui.CloseCurrentPopup();

        if (!ImGui.IsItemClicked())
            return;

        var items = LootRuleNames.ToArray();
        var request = new CollectionSelectorRequest
        (
            "选择战利品分配",
            "暂无可选战利品分配",
            Array.FindIndex(items, item => item.Key == current),
            items.Select(item => new CollectionSelectorItem(item.Value)).ToArray()
        );

        CollectionSelectorWindow.Open
        (
            request,
            index =>
            {
                if ((uint)index >= (uint)items.Length)
                    return;

                setCurrent(items[index].Key);
            }
        );
    }

    private static bool DrawFinderOptionCell(int columnIndex, string label, bool currentValue, Action<bool> assign)
    {
        ImGui.TableSetColumnIndex(columnIndex);

        var value   = currentValue;
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
