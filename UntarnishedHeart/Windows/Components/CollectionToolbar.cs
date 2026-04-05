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
        string          emptyText = "暂无数据"
    )
    {
        ImGui.AlignTextToFramePadding();
        ImGui.Text(label);

        selectedIndex = NormalizeSelectedIndex(selectedIndex, items.Count);

        if (items.Count == 0 || selectedIndex < 0)
        {
            ImGui.Text(emptyText);
            return;
        }

        var selectedItem = items[selectedIndex];

        ImGui.SameLine();
        ImGui.SetNextItemWidth(250f * GlobalUIScale);

        using (var combo = ImRaii.Combo(comboID, getName(selectedItem), ImGuiComboFlags.HeightLarge))
            if (combo)
            {
            }

        if (ImGui.IsItemClicked())
            ImGui.OpenPopup(comboID);

        using (var popup = ImRaii.PopupModal(comboID, ImGuiWindowFlags.AlwaysAutoResize))
        {
            if (popup)
            {
                for (var i = 0; i < items.Count; i++)
                {
                    var item = items[i];
                    if (ImGui.Selectable($"{getName(item)}###{comboID}-{i}"))
                        selectedIndex = i;

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
