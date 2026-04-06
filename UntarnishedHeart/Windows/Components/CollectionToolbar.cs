namespace UntarnishedHeart.Windows.Components;

internal static class CollectionToolbar
{
    public static int NormalizeSelectedIndex(int selectedIndex, int count)
    {
        if (count <= 0) return -1;
        return Math.Clamp(selectedIndex, 0, count - 1);
    }

    public static void DrawSelector<T>
    (
        string          label,
        string          comboID,
        IList<T>        items,
        ref int         selectedIndex,
        Func<T, string> getName,
        Action<T>?      onDelete  = null,
        string          emptyText = "暂无数据",
        float           itemWidth = 280f
    )
    {
        if (!string.IsNullOrEmpty(label))
        {
            ImGui.AlignTextToFramePadding();
            ImGui.Text(label);
        }

        selectedIndex = NormalizeSelectedIndex(selectedIndex, items.Count);

        if (items.Count == 0)
        {
            if (!string.IsNullOrEmpty(label))
                ImGui.SameLine();

            ImGui.TextDisabled(emptyText);
            return;
        }

        var selectedItem = selectedIndex >= 0 ? items[selectedIndex] : items[0];
        var previewValue = selectedIndex >= 0 ? getName(selectedItem) : "请选择";

        if (!string.IsNullOrEmpty(label))
            ImGui.SameLine();

        ImGui.SetNextItemWidth(itemWidth <= 0f ? itemWidth : itemWidth * GlobalUIScale);
        using (var combo = ImRaii.Combo(comboID, previewValue, ImGuiComboFlags.HeightLarge))
        {
            if (!combo)
                return;

            for (var i = 0; i < items.Count; i++)
            {
                var item       = items[i];
                var isSelected = selectedIndex == i;
                if (ImGui.Selectable($"{getName(item)}###{comboID}-{i}", isSelected))
                    selectedIndex = i;

                if (isSelected)
                    ImGui.SetItemDefaultFocus();

                if (onDelete == null)
                    continue;

                using var context = ImRaii.ContextPopupItem($"{comboID}-{i}ContextPopup");
                if (!context) continue;

                using var disabled = ImRaii.Disabled(items.Count <= 1);

                if (ImGui.MenuItem($"删除##{comboID}-{i}"))
                {
                    onDelete(item);
                    selectedIndex = NormalizeSelectedIndex(selectedIndex, items.Count);
                    return;
                }
            }
        }
    }

    public static void DrawActionButtons
    (
        string saveID,
        Action onSave,
        string addID,
        Action onAdd,
        string importID,
        Action onImport,
        string exportID,
        Action onExport,
        bool   canExport = true
    )
    {
        if (ImGuiOm.ButtonIcon(saveID, FontAwesomeIcon.Save, "保存", true))
            onSave();

        ImGui.SameLine();

        if (ImGuiOm.ButtonIcon(addID, FontAwesomeIcon.FileCirclePlus, "新增", true))
            onAdd();

        ImGui.SameLine();

        if (ImGuiOm.ButtonIcon(importID, FontAwesomeIcon.FileImport, "导入", true))
            onImport();

        ImGui.SameLine();

        using var disabled = ImRaii.Disabled(!canExport);
        if (ImGuiOm.ButtonIcon(exportID, FontAwesomeIcon.FileExport, "导出", true) && canExport)
            onExport();
    }
}
