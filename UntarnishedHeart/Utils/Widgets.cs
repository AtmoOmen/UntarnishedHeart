using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using ImGuiNET;
using Lumina.Excel.Sheets;

namespace UntarnishedHeart.Utils;

public static class Widgets
{
    public static bool ContentSelectCombo(ref uint selected, ref string contentSearchInput, 
                                          Dictionary<uint, ContentFinderCondition>? sourceData = null)
    {
        var selectState = false;

        var previewText =
            LuminaGetter.TryGetRow<TerritoryType>(selected, out var zone) && zone.ContentFinderCondition.IsValid
                ? $"{zone.ContentFinderCondition.Value.Name.ExtractText()}"
                : "未选择任何有效副本";
        
        if (ImGui.BeginCombo("###ContentSelectCombo", previewText, ImGuiComboFlags.HeightLarge))
            ImGui.EndCombo();

        if (ImGui.IsItemClicked())
            ImGui.OpenPopup("###ContentSelectPopup");

        ImGui.SetNextWindowSize(ImGuiHelpers.ScaledVector2(600f, 400f));
        using var popup = ImRaii.Popup("###ContentSelectPopup");
        if (popup)
        {
            ImGui.SetNextItemWidth(-1f);
            ImGui.InputTextWithHint("###ContentSearchInput", "请搜索", ref contentSearchInput, 32);

            ImGui.Separator();

            var tableSize = new Vector2(ImGui.GetContentRegionAvail().X, 0);
            using var table = ImRaii.Table("###ContentSelectTable", 5, ImGuiTableFlags.Borders, tableSize);
            if (table)
            {
                ImGui.TableSetupColumn("Checkbox", ImGuiTableColumnFlags.WidthFixed, ImGui.GetTextLineHeightWithSpacing());
                ImGui.TableSetupColumn("Icon", ImGuiTableColumnFlags.WidthFixed, 20f * ImGuiHelpers.GlobalScale);
                ImGui.TableSetupColumn("Level", ImGuiTableColumnFlags.WidthFixed, ImGui.CalcTextSize("等级数").X);
                ImGui.TableSetupColumn("DutyName", ImGuiTableColumnFlags.WidthStretch, 40);
                ImGui.TableSetupColumn("PlaceName", ImGuiTableColumnFlags.WidthStretch, 40);

                ImGui.TableNextRow(ImGuiTableRowFlags.Headers);
                ImGui.TableNextColumn();
                ImGui.TableNextColumn();
                ImGui.TableNextColumn();
                ImGui.Text("等级");
                ImGui.TableNextColumn();
                ImGui.Text("副本名");
                ImGui.TableNextColumn();
                ImGui.Text("区域名");

                var selectedCopy = selected;
                var data = (sourceData ?? PresetData.Contents)
                                     .OrderByDescending(x => selectedCopy == x.Key);
                foreach (var contentPair in data)
                {
                    var contentName = contentPair.Value.Name.ExtractText();
                    var placeName = contentPair.Value.TerritoryType.Value.PlaceName.Value.Name.ExtractText();
                    if (!string.IsNullOrWhiteSpace(contentSearchInput) &&
                        !contentName.Contains(contentSearchInput, StringComparison.OrdinalIgnoreCase) &&
                        !placeName.Contains(contentSearchInput, StringComparison.OrdinalIgnoreCase)) continue;

                    using var id = ImRaii.PushId($"{contentName}_{contentPair.Key}");
                    ImGui.TableNextRow();

                    ImGui.TableNextColumn();
                    var state = selected == contentPair.Key;
                    using (ImRaii.Disabled())
                        ImGui.RadioButton(string.Empty, state);

                    ImGui.TableNextColumn();
                    ImGui.Image(ImageHelper.GetGameIcon(contentPair.Value.ContentType.Value.Icon).ImGuiHandle,
                                ImGuiHelpers.ScaledVector2(20f));

                    ImGui.TableNextColumn();
                    ImGui.Text(contentPair.Value.ClassJobLevelRequired.ToString());

                    ImGui.TableNextColumn();
                    if (ImGui.Selectable(contentName, state, ImGuiSelectableFlags.SpanAllColumns | ImGuiSelectableFlags.DontClosePopups))
                    {
                        selected = contentPair.Key;
                        selectState = true;
                    }

                    var image = ImageHelper.GetGameIcon(contentPair.Value.Image);
                    if (image != null && ImGui.IsItemHovered())
                    {
                        using (ImRaii.Tooltip())
                            ImGui.Image(image.ImGuiHandle, image.Size / 2);
                    }

                    ImGui.TableNextColumn();
                    ImGui.Text(placeName);
                }
            }
        }

        return selectState;
    }
}
