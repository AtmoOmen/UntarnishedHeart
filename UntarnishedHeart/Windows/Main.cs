using System;
using System.Collections.Generic;
using System.Linq;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using ImGuiNET;
using Lumina.Excel.Sheets;
using OmenTools.Service;
using UntarnishedHeart.Managers;
using UntarnishedHeart.Executor;
using ContentsFinder = FFXIVClientStructs.FFXIV.Client.Game.UI.ContentsFinder;
using Status = Lumina.Excel.Sheets.Status;

namespace UntarnishedHeart.Windows;

public class Main() : Window($"{PluginName} {Plugin.Version}###{PluginName}-MainWindow"), IDisposable
{
    public static Executor.Executor? PresetExecutor { get; private set; }
    public static RouteExecutor? RouteExecutor { get; private set; }

    public static SeString UTHPrefix { get; } = new SeStringBuilder()
                                                .AddUiForeground(SeIconChar.BoxedLetterU.ToIconString(), 31)
                                                .AddUiForeground(SeIconChar.BoxedLetterT.ToIconString(), 31)
                                                .AddUiForeground(SeIconChar.BoxedLetterH.ToIconString(), 31)
                                                .AddUiForegroundOff().Build();

    public static readonly Dictionary<ContentsFinder.LootRule, string> LootRuleLOC = new()
    {
        [ContentsFinder.LootRule.Normal]     = "通常",
        [ContentsFinder.LootRule.GreedOnly]  = "仅限贪婪",
        [ContentsFinder.LootRule.Lootmaster] = "队长分配"
    };

    public static readonly Dictionary<uint, string> ZonePlaceNames =
        LuminaGetter.Get<TerritoryType>()
                    .Select(x => (x.RowId, x.ExtractPlaceName()))
                    .Where(x => !string.IsNullOrWhiteSpace(x.Item2))
                    .ToDictionary(x => x.RowId, x => x.Item2);

    private static int SelectedPresetIndex;
    private static int SelectedRouteIndex;


    public override void Draw()
    {
        if (SelectedPresetIndex >= Service.Config.Presets.Count || SelectedPresetIndex < 0)
            SelectedPresetIndex = 0;

        if (Service.Config.Presets.Count == 0)
        {
            Service.Config.Presets.Add(Configuration.ExamplePreset0);
            Service.Config.Presets.Add(Configuration.ExamplePreset1);
            Service.Config.Presets.Add(Configuration.ExamplePreset2);
            Service.Config.Save();
        }

        using var tabBar = ImRaii.TabBar("###MainTabBar");
        if (!tabBar) return;
        
        using (var mainPageItem = ImRaii.TabItem("主页"))
        {
            if (mainPageItem)
            {
                DrawExecutionModeSelector();

                ImGui.Separator();
                ImGui.Spacing();

                DrawHomeExecutorInfo();

                ImGui.Separator();
                ImGui.Spacing();

                if (Service.Config.CurrentExecutionMode == ExecutionMode.Simple)
                {
                    DrawHomeExecutorConfig();

                    ImGui.Separator();
                    ImGui.Spacing();

                    DrawHomeContentConfig();
                    
                    ImGui.Separator();
                    ImGui.Spacing();

                    using (ImRaii.Disabled(PresetExecutor is { IsDisposed: false } || BetweenAreas))
                    {
                        if (ImGuiOm.ButtonSelectable("开始"))
                        {
                            PresetExecutor?.Dispose();
                            PresetExecutor = null;

                            PresetExecutor ??= new(Service.Config.Presets[SelectedPresetIndex],
                                                   Service.Config.RunTimes);
                        }
                    }

                    if (ImGuiOm.ButtonSelectable("结束"))
                    {
                        PresetExecutor?.Dispose();
                        PresetExecutor = null;

                        // 如果在排本就取消
                        if (DService.Condition[ConditionFlag.InDutyQueue])
                        {
                            unsafe
                            {
                                SendEvent(AgentId.ContentsFinder, 0, 12, 0);
                            }
                        }
                    }
                }
                else
                {
                    DrawRouteExecutorConfig();

                    ImGui.Separator();
                    ImGui.Spacing();

                    using (ImRaii.Disabled(RouteExecutor is { IsRunning: true } || BetweenAreas))
                    {
                        if (ImGuiOm.ButtonSelectable("开始路线"))
                        {
                            if (Service.Config.Routes.Count > 0 && SelectedRouteIndex < Service.Config.Routes.Count)
                            {
                                RouteExecutor?.Stop();
                                RouteExecutor = new RouteExecutor(Service.Config.Routes[SelectedRouteIndex]);
                                RouteExecutor.Start();
                            }
                        }
                    }

                    if (ImGuiOm.ButtonSelectable("停止路线"))
                    {
                        RouteExecutor?.Stop();
                        RouteExecutor = null;

                        // 如果在排本就取消
                        if (DService.Condition[ConditionFlag.InDutyQueue])
                        {
                            unsafe
                            {
                                SendEvent(AgentId.ContentsFinder, 0, 12, 0);
                            }
                        }
                    }
                }
            }
        }

        if (ImGui.TabItemButton("预设"))
            WindowManager.Get<PresetEditor>().IsOpen ^= true;
            
        if (ImGui.TabItemButton("路线"))
            WindowManager.Get<RouteEditor>().IsOpen ^= true;
            
        if (ImGui.TabItemButton("调试"))
            WindowManager.Get<Debug>().IsOpen ^= true;
    }

    public override void OnClose() => Service.Config.Save();

    private static void DrawExecutionModeSelector()
    {
        ImGui.TextColored(LightBlue, "执行模式:");
        using var indent = ImRaii.PushIndent();

        var currentMode = Service.Config.CurrentExecutionMode;
        
        if (ImGui.RadioButton("简单模式", currentMode == ExecutionMode.Simple))
        {
            Service.Config.CurrentExecutionMode = ExecutionMode.Simple;
            Service.Config.Save();
            
            // 切换模式时停止当前执行
            RouteExecutor?.Stop();
            RouteExecutor = null;
        }
        
        ImGui.SameLine();
        if (ImGui.RadioButton("运行路线", currentMode == ExecutionMode.Route))
        {
            Service.Config.CurrentExecutionMode = ExecutionMode.Route;
            Service.Config.Save();
            
            // 切换模式时停止当前执行
            PresetExecutor?.Dispose();
            PresetExecutor = null;
        }
    }

    private static void DrawHomeExecutorInfo()
    {
        ImGui.TextColored(LightBlue, "运行状态:");
        using var indent = ImRaii.PushIndent();

        ImGui.Text("当前状态:");

        ImGui.SameLine();
        if (Service.Config.CurrentExecutionMode == ExecutionMode.Simple)
        {
            ImGui.TextColored(PresetExecutor == null || PresetExecutor.IsDisposed ? ImGuiColors.DalamudRed : ImGuiColors.ParsedGreen,
                              PresetExecutor == null || PresetExecutor.IsDisposed ? "等待中" : "运行中");

            ImGui.Text("运行次数:");
            ImGui.SameLine();
            ImGui.Text($"{PresetExecutor?.CurrentRound ?? 0} / {PresetExecutor?.MaxRound ?? 0}");

            ImGui.Text("运行信息:");
            ImGui.SameLine();
            ImGui.TextWrapped($"{PresetExecutor?.RunningMessage ?? string.Empty}");
        }
        else
        {
            ImGui.TextColored(RouteExecutor?.IsRunning == true ? ImGuiColors.ParsedGreen : ImGuiColors.DalamudRed,
                              RouteExecutor?.IsRunning == true ? "运行中" : "等待中");

            ImGui.Text("当前步骤:");
            ImGui.SameLine();
            ImGui.Text($"{(RouteExecutor?.CurrentStepIndex ?? -1)} / {(RouteExecutor?.Steps.Count ?? 0) -1}");

            ImGui.Text("运行信息:");
            ImGui.SameLine();
            ImGui.TextWrapped($"{RouteExecutor?.RunningMessage ?? string.Empty}");
        }
    }
    
    private static void DrawHomeExecutorConfig()
    {
        ImGui.TextColored(LightBlue, "运行设置:");
        
        using var indent = ImRaii.PushIndent();
        using var group  = ImRaii.Group();

        var selectedPreset = Service.Config.Presets[SelectedPresetIndex];
        ImGui.SetNextItemWidth(200f * ImGuiHelpers.GlobalScale);
        using (var combo = ImRaii.Combo("选择预设###PresetSelectCombo", $"{selectedPreset.Name}", ImGuiComboFlags.HeightLarge))
        {
            if (combo)
            {
                for (var i = 0; i < Service.Config.Presets.Count; i++)
                {
                    var preset = Service.Config.Presets[i];
                    if (ImGui.Selectable($"{preset.Name}###{preset}-{i}"))
                        SelectedPresetIndex = i;

                    using var popup = ImRaii.ContextPopupItem($"{preset}-{i}ContextPopup");
                    if (popup)
                    {
                        using (ImRaii.Disabled(Service.Config.Presets.Count == 1))
                        {
                            if (ImGui.MenuItem($"删除##{preset}-{i}"))
                                Service.Config.Presets.Remove(preset);
                        }
                    }
                }
            }
        }

        var runTimes = Service.Config.RunTimes;
        ImGui.SetNextItemWidth(200f * ImGuiHelpers.GlobalScale);
        ImGui.InputInt("运行次数###RunTimes", ref runTimes, 0, 0);
        if (ImGui.IsItemDeactivatedAfterEdit())
        {
            Service.Config.RunTimes = runTimes;
            Service.Config.Save();
        }
        ImGuiOm.TooltipHover("若输入 -1, 则为无限运行");

        var isLeaderMode = Service.Config.LeaderMode;
        if (ImGui.Checkbox("队长模式", ref isLeaderMode))
        {
            Service.Config.LeaderMode = isLeaderMode;
            Service.Config.Save();
        }
        ImGuiOm.TooltipHover("启用队长模式时, 副本结束后会自动尝试排入同一副本", 20f * ImGuiHelpers.GlobalScale);
        ImGui.SameLine();
        var isAutoRecommendGear = Service.Config.AutoRecommendGear;
        if (ImGui.Checkbox("自动最强", ref isAutoRecommendGear))
        {
            Service.Config.AutoRecommendGear = isAutoRecommendGear;
            Service.Config.Save();
        }
        ImGuiOm.TooltipHover("启用自动最强时，进入副本时会尝试装备当前职业最强装备", 20f * ImGuiHelpers.GlobalScale);
    }

    private static void DrawHomeContentConfig()
    {
        ImGui.TextColored(LightBlue, "副本选项:");
        
        using var indent = ImRaii.PushIndent();
        using var group  = ImRaii.Group();
        
        using (ImRaii.Group())
        {
            var isUnrest = Service.Config.ContentsFinderOption.UnrestrictedParty;
            if (ImGui.Checkbox("解除限制", ref isUnrest))
            {
                var newOption = Service.Config.ContentsFinderOption.Clone();
                newOption.UnrestrictedParty = isUnrest;

                Service.Config.ContentsFinderOption = newOption;
                Service.Config.Save();
            }

            ImGui.SameLine();
            var isSync = Service.Config.ContentsFinderOption.LevelSync;
            if (ImGui.Checkbox("等级同步", ref isSync))
            {
                var newOption = Service.Config.ContentsFinderOption.Clone();
                newOption.LevelSync = isSync;

                Service.Config.ContentsFinderOption = newOption;
                Service.Config.Save();
            }

            ImGui.SameLine();
            var isMinIL = Service.Config.ContentsFinderOption.MinimalIL;
            if (ImGui.Checkbox("最低品级", ref isMinIL))
            {
                var newOption = Service.Config.ContentsFinderOption.Clone();
                newOption.MinimalIL = isMinIL;

                Service.Config.ContentsFinderOption = newOption;
                Service.Config.Save();
            }

            ImGui.SameLine();
            var isNoEcho = Service.Config.ContentsFinderOption.SilenceEcho;
            if (ImGui.Checkbox("超越之力无效化", ref isNoEcho))
            {
                var newOption = Service.Config.ContentsFinderOption.Clone();
                newOption.SilenceEcho = isNoEcho;

                Service.Config.ContentsFinderOption = newOption;
                Service.Config.Save();
            }

            var lootRule = Service.Config.ContentsFinderOption.LootRules;
            var isFirst = true;
            foreach (var (loot, loc) in LootRuleLOC)
            {
                if (!isFirst)
                    ImGui.SameLine();
                isFirst = false;
                
                if (ImGui.RadioButton($"{loc}##{loot}", loot == lootRule))
                {
                    var newOption = Service.Config.ContentsFinderOption.Clone();
                    newOption.LootRules = loot;

                    Service.Config.ContentsFinderOption = newOption;
                    Service.Config.Save();
                }
            }
        }
        
        var contentEntry = Service.Config.ContentEntryType;
        ImGui.SetNextItemWidth(200f * ImGuiHelpers.GlobalScale);
        using (var combo = ImRaii.Combo("副本入口###ContentEntryCombo", contentEntry.GetDescription()))
        {
            if (combo)
            {
                foreach (var entryType in Enum.GetValues<ContentEntryType>())
                {
                    if (ImGui.Selectable(entryType.GetDescription(), entryType == contentEntry))
                    {
                        Service.Config.ContentEntryType = entryType;
                        Service.Config.Save();
                    }
                }
            }
        }
        ImGuiOm.TooltipHover("单人进入多变迷宫:\n\t勾选解除限制, 入口选择一般副本");
    }
    
    private static void DrawRouteExecutorConfig()
    {
        ImGui.TextColored(LightBlue, "路线设置:");
        
        using var indent = ImRaii.PushIndent();
        using var group  = ImRaii.Group();

        if (Service.Config.Routes.Count == 0)
        {
            ImGui.Text("暂无路线，请先创建路线");
            return;
        }

        if (SelectedRouteIndex >= Service.Config.Routes.Count || SelectedRouteIndex < 0)
            SelectedRouteIndex = 0;

        var selectedRoute = Service.Config.Routes[SelectedRouteIndex];
        ImGui.SetNextItemWidth(200f * ImGuiHelpers.GlobalScale);
        using (var combo = ImRaii.Combo("选择路线###RouteSelectCombo", $"{selectedRoute.Name}", ImGuiComboFlags.HeightLarge))
        {
            if (combo)
            {
                for (var i = 0; i < Service.Config.Routes.Count; i++)
                {
                    var route = Service.Config.Routes[i];
                    if (ImGui.Selectable($"{route.Name}###{route}-{i}"))
                        SelectedRouteIndex = i;

                    using var popup = ImRaii.ContextPopupItem($"{route}-{i}ContextPopup");
                    if (popup)
                    {
                        using (ImRaii.Disabled(Service.Config.Routes.Count == 1))
                        {
                            if (ImGui.MenuItem($"删除##{route}-{i}"))
                                Service.Config.Routes.Remove(route);
                        }
                    }
                }
            }
        }

        // 显示路线信息
        if (!string.IsNullOrWhiteSpace(selectedRoute.Note))
        {
            ImGui.Text($"备注: {selectedRoute.Note}");
        }
        
        ImGui.Text($"步骤数: {selectedRoute.Steps.Count}");
    }

    public void Dispose()
    {
        PresetExecutor?.Dispose();
        PresetExecutor = null;
        
        RouteExecutor?.Stop();
        RouteExecutor = null;
    }
}
