using System;
using System.Collections.Generic;
using System.Linq;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using ImGuiNET;
using Lumina.Excel.GeneratedSheets;
using UntarnishedHeart.Managers;
using UntarnishedHeart.Executor;

namespace UntarnishedHeart.Windows;

public class Main() : Window($"{PluginName} 主界面###{PluginName}-MainWindow", ImGuiWindowFlags.AlwaysAutoResize), IDisposable
{
    private static Executor.Executor? PresetExecutor;

    private static int SelectedPresetIndex;
    private static bool IsSelectorDisplay;

    public static readonly Dictionary<uint, string> ZonePlaceNames;

    static Main()
    {
        ZonePlaceNames = LuminaCache.Get<TerritoryType>()
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
            Service.Config.Save();
        }

        DrawExecutorInfo();

        ImGui.Separator();
        ImGui.Spacing();

        DrawNesscaryInfo();

        ImGui.Separator();
        ImGui.Spacing();

        DrawExecutorConfig();

        ImGui.Separator();
        ImGui.Spacing();

        using (ImRaii.Disabled(PresetExecutor is { IsDisposed: false }))
        {
            if (ImGuiOm.ButtonSelectable("开始"))
            {
                PresetExecutor?.Dispose();
                PresetExecutor = null;

                PresetExecutor ??= new(Service.Config.Presets[SelectedPresetIndex],
                                       Service.Config.RunTimes,
                                       Service.Config.AutoOpenTreasure,
                                       Service.Config.LeaveDutyDelay);
            }

            if (Service.Config.LeaderMode)
                ImGuiOm.TooltipHover("你已开启队长模式, 请阅读并确认下列注意事项:\n" +
                                     "1. 确认并在任务搜索器内选取完成你所选择的副本\n" +
                                     "2. 配置好相关的任务搜索器设置 (如: 解除限制)\n" +
                                     "3. 首次运作需要你手动排本, 后续为插件自动排本\n");
        }

        if (ImGuiOm.ButtonSelectable("结束"))
        {
            PresetExecutor?.Dispose();
            PresetExecutor = null;
        }

        if (!IsSelectorDisplay) return;

        var windowWidth = ImGui.GetWindowWidth();
        var windowPos = ImGui.GetWindowPos();
        ImGui.SetNextWindowPos(windowPos with { X = windowPos.X + windowWidth });
        if (ImGui.Begin($"预设选择器###{PluginName}-PresetSelector", ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.AlwaysAutoResize))
        {
            ImGui.AlignTextToFramePadding();
            ImGui.Text("选择预设:");

            ImGui.SameLine();
            ImGui.SetNextItemWidth(150f * ImGuiHelpers.GlobalScale);

            if (SelectedPresetIndex > Service.Config.Presets.Count - 1)
                SelectedPresetIndex = 0;

            var selectedPreset = Service.Config.Presets[SelectedPresetIndex];
            using (var combo = ImRaii.Combo("###PresetSelectCombo", $"{selectedPreset.Name}", ImGuiComboFlags.HeightLarge))
            {
                if (combo)
                {
                    for (var i = 0; i < Service.Config.Presets.Count; i++)
                    {
                        var preset = Service.Config.Presets[i];
                        if (ImGui.Selectable($"{preset.Name}###{preset}-{i}"))
                            SelectedPresetIndex = i;

                        using var popup = ImRaii.ContextPopup($"{preset}-{i}ContextPopup");
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

            ImGui.SameLine();
            if (ImGuiOm.ButtonIcon("AddNewPreset", FontAwesomeIcon.FileCirclePlus, "添加预设", true))
            {
                Service.Config.Presets.Add(new());
                SelectedPresetIndex = Service.Config.Presets.Count - 1;
            }

            ImGui.SameLine();
            if (ImGuiOm.ButtonIcon("ImportNewPreset", FontAwesomeIcon.FileImport, "导入预设", true))
            {
                var config = ExecutorPreset.ImportFromClipboard();
                if (config != null)
                {
                    Service.Config.Presets.Add(config);
                    Service.Config.Save();

                    SelectedPresetIndex = Service.Config.Presets.Count - 1;
                }
            }

            ImGui.SameLine();
            if (ImGuiOm.ButtonIcon("ExportPreset", FontAwesomeIcon.FileExport, "导出预设", true))
            {
                var selectedPresetExported = Service.Config.Presets[SelectedPresetIndex];
                selectedPresetExported.ExportToClipboard();
            }

            ImGui.Separator();
            ImGui.Spacing();

            selectedPreset.Draw();

            ImGui.End();
        }
    }

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
        ImGui.Text($"{PresetExecutor?.RunningMessage ?? string.Empty}");
    }

    private static void DrawNesscaryInfo()
    {
        ImGui.TextColored(LightBlue, "必要信息:");
        using var indent = ImRaii.PushIndent();

        ImGui.Text("当前区域:");

        var zoneName = ZonePlaceNames.GetValueOrDefault(DService.ClientState.TerritoryType, "未知区域");
        ImGui.SameLine();
        ImGui.Text($"{zoneName} ({DService.ClientState.TerritoryType})");
        ImGui.Text("当前目标:");

        var target = DService.Targets.Target;
        ImGui.SameLine();
        ImGui.Text(target is not { ObjectKind:ObjectKind.BattleNpc } ? string.Empty : $"{target.Name} (DataID: {target.DataId})");

        ImGui.Text("当前位置:");

        ImGui.SameLine();
        ImGui.Text($"{DService.ClientState.LocalPlayer?.Position:F2}");
    }

    private static void DrawExecutorConfig()
    {
        ImGui.TextColored(LightBlue, "运行设置:");
        using var indent = ImRaii.PushIndent();

        using (ImRaii.Group())
        {
            var selectedPreset = Service.Config.Presets[SelectedPresetIndex];

            ImGui.AlignTextToFramePadding();
            ImGui.Text("已选预设:");

            ImGui.SameLine();
            ImGui.Text($"{selectedPreset.Name}");

            ImGui.AlignTextToFramePadding();
            ImGui.Text("移动方式:");

            foreach (var moveType in Enum.GetValues<MoveType>())
            {
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

            var autoOpenTreasure = Service.Config.AutoOpenTreasure;
            if (ImGui.Checkbox("副本结束时, 自动开启宝箱", ref autoOpenTreasure))
            {
                Service.Config.AutoOpenTreasure = autoOpenTreasure;
                Service.Config.Save();
            }
            ImGuiOm.HelpMarker("请确保目标副本的确有宝箱, 否则流程将卡死", 20f, FontAwesomeIcon.InfoCircle, true);

            var leaveDutyDelay = (int)Service.Config.LeaveDutyDelay;
            ImGui.SetNextItemWidth(125f * ImGuiHelpers.GlobalScale);
            if (ImGui.InputInt("退本延迟 (ms)", ref leaveDutyDelay))
                Service.Config.LeaveDutyDelay = (uint)Math.Max(0, leaveDutyDelay);
            if (ImGui.IsItemDeactivatedAfterEdit())
                Service.Config.Save();
        }

        var groupSize = ImGui.GetItemRectSize();

        ImGui.SameLine();
        if (ImGuiOm.ButtonIconWithTextVertical(FontAwesomeIcon.Eye, "选择预设",
                                               groupSize with { X = ImGui.CalcTextSize("选择预设").X * 1.5f }, true))
            IsSelectorDisplay ^= true;
    }

    public void Dispose()
    {
        PresetExecutor?.Dispose();
        PresetExecutor = null;
    }
}
