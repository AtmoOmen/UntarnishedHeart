using System;
using System.Collections.Generic;
using System.Linq;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using ImGuiNET;
using Lumina.Excel.Sheets;
using UntarnishedHeart.Managers;
using UntarnishedHeart.Executor;

namespace UntarnishedHeart.Windows;

public class Main() : Window($"{PluginName} {Plugin.Version}###{PluginName}-MainWindow"), IDisposable
{
    public static Executor.Executor? PresetExecutor { get; private set; }

    public static readonly Dictionary<uint, string> ZonePlaceNames;
    
    private static int  SelectedPresetIndex;
    private static bool IsDrawConfig = true;

    static Main()
    {
        ZonePlaceNames = LuminaGetter.Get<TerritoryType>()
                                    .Select(x => (x.RowId, x.ExtractPlaceName()))
                                    .Where(x => !string.IsNullOrWhiteSpace(x.Item2))
                                    .ToDictionary(x => x.RowId, x => x.Item2);
    }

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
                DrawExecutorInfo();

                ImGui.Separator();
                ImGui.Spacing();

                DrawExecutorConfig();

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

                    if (Service.Config.LeaderMode)
                        ImGuiOm.TooltipHover("你已开启队长模式, 请阅读并确认下列注意事项:\n\n"  +
                                             "1. 在任务搜索器内选取完成你所选择的副本\n"      +
                                             "2. 配置好相关的任务搜索器设置 (如: 解除限制)\n" +
                                             "3. 首次运作需要你手动排本, 后续为插件自动排本\n"  +
                                             "4. 如果你目标副本是多变迷宫且为单人进本, 请先在任务搜索器内启用解除限制, 不然无法进本");
                }

                if (ImGuiOm.ButtonSelectable("结束"))
                {
                    PresetExecutor?.Dispose();
                    PresetExecutor = null;
                }
            }
        }

        using (var debugPageItem = ImRaii.TabItem("调试"))
        {
            if (debugPageItem)
            {
                DrawDebugGeneralInfo();

                ImGui.Separator();
                ImGui.Spacing();

                DrawDebugTargetInfo();
                
                ImGui.Separator();
                ImGui.Spacing();

                DrawDebugStatusInfo();
            }
        }
    }

    public override void OnClose() => Service.Config.Save();

    private static void DrawExecutorInfo()
    {
        ImGui.TextColored(LightBlue, "运行状态:");
        using var indent = ImRaii.PushIndent();

        ImGui.Text("当前状态:");

        ImGui.SameLine();
        ImGui.TextColored(PresetExecutor == null || PresetExecutor.IsDisposed ? ImGuiColors.DalamudRed : ImGuiColors.ParsedGreen,
                          PresetExecutor == null || PresetExecutor.IsDisposed ? "等待中" : "运行中");

        ImGui.SameLine();
        ImGui.TextDisabled("|");

        ImGui.SameLine();
        ImGui.Text("次数:");

        ImGui.SameLine();
        ImGui.Text($"{PresetExecutor?.CurrentRound ?? 0} / {PresetExecutor?.MaxRound ?? 0}");

        ImGui.Text("运行信息:");

        ImGui.SameLine();
        ImGui.TextWrapped($"{PresetExecutor?.RunningMessage ?? string.Empty}");
    }
    
    private static void DrawExecutorConfig()
    {
        ImGui.TextColored(LightBlue, "运行设置:");
        if (ImGui.IsItemHovered())
            ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
        if (ImGui.IsItemClicked())
            IsDrawConfig ^= true;
        
        if (!IsDrawConfig) return;
        
        using var indent = ImRaii.PushIndent();

        using (ImRaii.Group())
        {
            var selectedPreset = Service.Config.Presets[SelectedPresetIndex];

            ImGui.AlignTextToFramePadding();
            ImGui.Text("已选预设:");

            ImGui.SameLine();
            using (ImRaii.Group())
            {
                ImGui.SetNextItemWidth(150f * ImGuiHelpers.GlobalScale);
                using (var combo = ImRaii.Combo("###PresetSelectCombo", $"{selectedPreset.Name}", ImGuiComboFlags.HeightLarge))
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
            }

            ImGui.AlignTextToFramePadding();
            ImGui.Text("移动方式:");

            foreach (var moveType in Enum.GetValues<MoveType>())
            {
                if (moveType == MoveType.无) continue;
                
                ImGui.SameLine();
                if (ImGui.RadioButton(moveType.ToString(), moveType == Service.Config.MoveType))
                {
                    Service.Config.MoveType = moveType;
                    Service.Config.Save();
                }
            }

            var runTimes = Service.Config.RunTimes;
            if (ImGuiOm.CompLabelLeft("运行次数:", 50f * ImGuiHelpers.GlobalScale,
                                      () => ImGui.InputInt("###", ref runTimes, 0, 0)))
            {
                Service.Config.RunTimes = runTimes;
                Service.Config.Save();
            }
            ImGuiOm.TooltipHover("若输入 -1, 则为无限运行");

            ImGui.SameLine();
            var isLeaderMode = Service.Config.LeaderMode;
            if (ImGui.Checkbox("队长模式", ref isLeaderMode))
            {
                Service.Config.LeaderMode = isLeaderMode;
                Service.Config.Save();
            }
            ImGuiOm.HelpMarker("启用队长模式时, 副本结束后会自动尝试排入同一副本", 20f, FontAwesomeIcon.InfoCircle, true);
        }

        var groupSize = ImGui.GetItemRectSize();

        ImGui.SameLine();
        if (ImGuiOm.ButtonIconWithTextVertical(FontAwesomeIcon.Eye, "编辑预设",
                                               groupSize with { X = ImGui.CalcTextSize("编辑预设").X * 1.5f }, true))
            WindowManager.Get<PresetEditor>().IsOpen ^= true;
    }
    
    private static void DrawDebugGeneralInfo()
    {
        ImGui.TextColored(LightBlue, "一般信息:");
        using var indent = ImRaii.PushIndent();

        using (ImRaii.Group())
        {
            var isCurrentZoneValid = LuminaGetter.TryGetRow<TerritoryType>(DService.ClientState.TerritoryType, out var zoneRow);
            
            ImGui.Text("当前区域:");
            
            var zoneName = LuminaWarpper.GetZonePlaceName(DService.ClientState.TerritoryType);
            ImGui.SameLine();
            ImGui.Text($"{zoneName} ({DService.ClientState.TerritoryType})");

            if (isCurrentZoneValid)
            {
                ImGui.Text("副本区域:");

                var contentName = LuminaWarpper.GetContentName(zoneRow.ContentFinderCondition.RowId);
                ImGui.SameLine();
                ImGui.Text($"{contentName} ({zoneRow.ContentFinderCondition.RowId})");
                
                ImGui.Text("副本用途:");
                
                ImGui.SameLine();
                ImGui.Text($"{zoneRow.TerritoryIntendedUse.RowId}");
            }
        }

        using (ImRaii.Group())
        {
            ImGui.Text("当前位置:");

            ImGui.SameLine();
            ImGui.Text($"{DService.ClientState.LocalPlayer?.Position:F2}");
        }
    }

    private static void DrawDebugTargetInfo()
    {
        ImGui.TextColored(LightBlue, "目标信息:");
        using var indent = ImRaii.PushIndent();
        using var group = ImRaii.Group();

        if (DService.Targets.Target is IBattleChara target)
        {
            ImGui.Text("当前目标:");

            ImGui.SameLine();
            ImGui.Text($"{target.Name} (0x{target.Address:X})");

            ImGui.Text("目标类型:");

            ImGui.SameLine();
            ImGui.Text($"{target.ObjectKind} ({(byte)target.ObjectKind})");

            ImGui.Text("Data ID:");

            ImGui.SameLine();
            ImGui.Text($"{target.DataId}");

            ImGui.SameLine(0, 10f * ImGuiHelpers.GlobalScale);
            ImGui.Text("Entity ID:");

            ImGui.SameLine();
            ImGui.Text($"{target.EntityId}");

            ImGui.Text("目标位置:");

            ImGui.SameLine();
            ImGui.Text($"{target.Position:F2}");
            
            ImGui.Text("目标 HP:");
            
            ImGui.SameLine();
            ImGui.Text($"{(double)target.CurrentHp / target.MaxHp * 100:F2}%% ({target.CurrentHp} / {target.MaxHp})");
        }
    }

    private static void DrawDebugStatusInfo()
    {
        ImGui.TextColored(LightBlue, "状态效果信息:");
        using var indent = ImRaii.PushIndent();
        using var group  = ImRaii.Group();
        
        if (DService.ClientState.LocalPlayer is { } localPlayer)
        {
            using (ImRaii.Group())
            {
                ImGui.Text("自身");

                foreach (var status in localPlayer.StatusList)
                {
                    if (!LuminaGetter.TryGetRow<Status>(status.StatusId, out var row)) continue;
                    if (!DService.Texture.TryGetFromGameIcon(new(row.Icon), out var iconTexture)) continue;

                    ImGui.Image(iconTexture.GetWrapOrEmpty().ImGuiHandle, ImGuiHelpers.ScaledVector2(24f));

                    ImGui.SameLine();
                    ImGui.AlignTextToFramePadding();
                    ImGui.Text($"{row.Name} ({row.RowId})");
                }
            }
        }
        
        if (DService.Targets.Target is IBattleChara target)
        {
            ImGui.SameLine();
            using (ImRaii.Group())
            {
                ImGui.Text("目标");

                foreach (var status in target.StatusList)
                {
                    if (!LuminaGetter.TryGetRow<Status>(status.StatusId, out var row)) continue;
                    if (!DService.Texture.TryGetFromGameIcon(new(row.Icon), out var iconTexture)) continue;

                    ImGui.Image(iconTexture.GetWrapOrEmpty().ImGuiHandle, ImGuiHelpers.ScaledVector2(24f));

                    ImGui.SameLine();
                    ImGui.AlignTextToFramePadding();
                    ImGui.Text($"{row.Name} ({row.RowId})");
                }
            }
        }
    }


    public void Dispose()
    {
        PresetExecutor?.Dispose();
        PresetExecutor = null;
    }
}
