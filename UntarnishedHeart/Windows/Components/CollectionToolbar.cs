namespace UntarnishedHeart.Windows.Components;

internal static class CollectionToolbar
{
    public static int NormalizeSelectedIndex(int selectedIndex, int count)
    {
        if (count <= 0) return -1;
        return Math.Clamp(selectedIndex, 0, count - 1);
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
