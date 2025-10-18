using System;
using System.Drawing;
using System.Numerics;
using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;
using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState.Keys;
using Lumina.Excel.Sheets;
using OmenTools.Service;
using OmenTools.Helpers;
using UntarnishedHeart.Utils;
using Status = Lumina.Excel.Sheets.Status;

namespace UntarnishedHeart.Windows;

public class Debug() : Window($"调试窗口###{PluginName}-DebugWindow"), IDisposable
{
    private static long lastCopyTime = 0;

    public override void Draw()
    {
        using var tabBar = ImRaii.TabBar("###DebugTabBar");
        if (!tabBar) return;
        
        using (var generalTabItem = ImRaii.TabItem("一般信息"))
        {
            if (generalTabItem)
            {
                DrawDebugGeneralInfo();
            }
        }
        
        using (var targetTabItem = ImRaii.TabItem("目标信息"))
        {
            if (targetTabItem)
            {
                DrawDebugTargetInfo();
            }
        }
        
        using (var statusTabItem = ImRaii.TabItem("状态效果信息"))
        {
            if (statusTabItem)
            {
                DrawDebugStatusInfo();
            }
        }

        using (var cursorTabItem = ImRaii.TabItem("鼠标位置转换"))
        {
            if (cursorTabItem)
            {
                DrawCursorToWorld();
            }
        }
    }
    
    private static void DrawDebugGeneralInfo()
    {
        if (ImGui.BeginTable("GeneralInfoTable", 2, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg))
        {
            ImGui.TableSetupColumn("属性", ImGuiTableColumnFlags.WidthFixed, 120);
            ImGui.TableSetupColumn("值", ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableHeadersRow();

            var isCurrentZoneValid = LuminaGetter.TryGetRow<TerritoryType>(DService.ClientState.TerritoryType, out var zoneRow);
            
            // 当前区域
            var zoneName = LuminaWrapper.GetZonePlaceName(DService.ClientState.TerritoryType);
            var zoneValue = $"{zoneName} ({DService.ClientState.TerritoryType})";
            DrawTableRow("当前区域", zoneValue);

            if (isCurrentZoneValid)
            {
                // 副本区域
                var contentName = LuminaWrapper.GetContentName(zoneRow.ContentFinderCondition.RowId);
                var contentValue = $"{contentName} ({zoneRow.ContentFinderCondition.RowId})";
                DrawTableRow("副本区域", contentValue);
                
                // 副本用途
                var territoryUseValue = $"{zoneRow.TerritoryIntendedUse.RowId}";
                DrawTableRow("副本用途", territoryUseValue);
            }

            // 当前位置
            var positionValue = $"{DService.ObjectTable.LocalPlayer?.Position:F2}";
            DrawTableRow("当前位置", positionValue);

            ImGui.EndTable();
        }
    }

    private static void DrawDebugTargetInfo()
    {
        if (DService.Targets.Target is not IBattleChara target)
        {
            ImGui.Text("无目标");
            return;
        }

        if (ImGui.BeginTable("TargetInfoTable", 2, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg))
        {
            ImGui.TableSetupColumn("属性", ImGuiTableColumnFlags.WidthFixed, 120);
            ImGui.TableSetupColumn("值",  ImGuiTableColumnFlags.WidthStretch);
            
            ImGui.TableHeadersRow();

            // 当前目标
            var targetValue = $"{target.Name} (0x{target.Address:X})";
            DrawTableRow("当前目标", targetValue);

            // 目标类型
            var objectKindValue = $"{target.ObjectKind} ({(byte)target.ObjectKind})";
            DrawTableRow("目标类型", objectKindValue);

            // Data ID
            var dataIdValue = $"{target.DataID}";
            DrawTableRow("Data ID", dataIdValue);

            // Entity ID
            var entityIdValue = $"{target.EntityID}";
            DrawTableRow("Entity ID", entityIdValue);

            // 目标位置
            var positionValue = $"{target.Position:F2}";
            DrawTableRow("目标位置", positionValue);

            // 目标体力
            var hpValue = $"{(double)target.CurrentHp / target.MaxHp * 100:F2}% ({target.CurrentHp} / {target.MaxHp})";
            DrawTableRow("目标体力", hpValue);

            if (target.IsCasting)
            {
                // 咏唱技能
                var castActionValue = $"{LuminaWrapper.GetActionName(target.CastActionID)} ({target.CastActionID} / {target.CastActionType})";
                DrawTableRow("咏唱技能", castActionValue);

                // 咏唱时间
                var castTimeValue = $"{target.CurrentCastTime:F2} / {target.TotalCastTime:F2}";
                DrawTableRow("咏唱时间", castTimeValue);
            }

            ImGui.EndTable();
        }
    }

    private static void DrawDebugStatusInfo()
    {
        using var group  = ImRaii.Group();
        
        if (DService.ObjectTable.LocalPlayer is { } localPlayer)
        {
            using (ImRaii.Group())
            {
                ImGui.TextColored(KnownColor.LightSkyBlue.Vector(), "自身");

                foreach (var status in localPlayer.StatusList)
                {
                    if (!LuminaGetter.TryGetRow<Status>(status.StatusID, out var row)) continue;
                    if (!DService.Texture.TryGetFromGameIcon(new(row.Icon), out var iconTexture)) continue;

                    ImGui.Image(iconTexture.GetWrapOrEmpty().Handle, ImGuiHelpers.ScaledVector2(24f));

                    ImGui.SameLine();
                    ImGui.AlignTextToFramePadding();
                    ImGui.Text($"{row.Name} ({row.RowId})");
                }
            }
        }
        
        ImGui.NewLine();
        
        if (DService.Targets.Target is IBattleChara target)
        {
            using (ImRaii.Group())
            {
                ImGui.TextColored(KnownColor.LightSkyBlue.Vector(), "目标");

                foreach (var status in target.StatusList)
                {
                    if (!LuminaGetter.TryGetRow<Status>(status.StatusID, out var row)) continue;
                    if (!DService.Texture.TryGetFromGameIcon(new(row.Icon), out var iconTexture)) continue;

                    ImGui.Image(iconTexture.GetWrapOrEmpty().Handle, ImGuiHelpers.ScaledVector2(24f));

                    ImGui.SameLine();
                    ImGui.AlignTextToFramePadding();
                    ImGui.Text($"{row.Name} ({row.RowId})");
                }
            }
        }
    }

    private static unsafe void DrawCursorToWorld()
    {
        var mousePos = ImGui.GetMousePos();
        ImGui.Text($"屏幕坐标: X={mousePos.X:F0}, Y={mousePos.Y:F0}");

        ImGui.Spacing();
        
        var camera = FFXIVClientStructs.FFXIV.Client.Game.Control.CameraManager.Instance()->GetActiveCamera();
        if (camera == null) return;
        
        var success = DService.Gui.ScreenToWorld(mousePos, out var worldPos);

        if (success)
        {
            var coordText = $"X: {worldPos.X:F2}, Y: {worldPos.Y:F2}, Z: {worldPos.Z:F2}";
            ImGui.Text("游戏世界坐标:");
            ImGui.SameLine();
            ImGui.TextColored(ImGuiColors.ParsedGreen, coordText);

            ImGui.Spacing();
            
            if (DService.KeyState[VirtualKey.CONTROL] && DService.KeyState[VirtualKey.C] &&
                Environment.TickCount64 - lastCopyTime > 500)
            {
                ImGui.SetClipboardText(coordText);
                lastCopyTime = Environment.TickCount64;
                NotifyHelper.NotificationSuccess($"已复制坐标到剪贴板: <{worldPos.X:F2}, {worldPos.Y:F2}, {worldPos.Z:F2}>");
            }

            if (ImGui.Button("复制 (Ctrl + C)"))
            {
                ImGui.SetClipboardText(coordText);
                NotifyHelper.NotificationSuccess($"已复制坐标到剪贴板: <{worldPos.X:F2}, {worldPos.Y:F2}, {worldPos.Z:F2}>");
            }

            ImGui.Spacing();
            ImGui.Separator();
            
            if (DService.ObjectTable.LocalPlayer is { } localPlayer)
            {
                var distanceToPlayer = Vector3.Distance(localPlayer.Position, worldPos);
                ImGui.Text($"距离玩家: {distanceToPlayer:F2}");
            }
        }
    }

    private static void DrawTableRow(string label, string value)
    {
        ImGui.TableNextRow();
        ImGui.TableSetColumnIndex(0);
        ImGui.Text(label);

        ImGui.TableSetColumnIndex(1);
        if (ImGui.Selectable(value, false, ImGuiSelectableFlags.SpanAllColumns))
        {
            ImGui.SetClipboardText(value);
        }

        if (ImGui.IsItemHovered())
        {
            ImGui.SetTooltip("点击复制到剪贴板");
        }
    }

    public void Dispose()
    {
        // 清理资源
    }
}
