namespace UntarnishedHeart.Windows.Components;

internal static class CollectionToolbar
{
    private static readonly Dictionary<string, SelectorTaskState> SelectorTasks = [];

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
        selectedIndex = NormalizeSelectedIndex(selectedIndex, items.Count);
        ConsumePendingResult(comboID, items, ref selectedIndex, onDelete);

        if (!string.IsNullOrEmpty(label))
        {
            ImGui.AlignTextToFramePadding();
            ImGui.Text(label);
        }

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

        DrawSelectorCombo(label, comboID, items, selectedIndex, getName, onDelete != null, emptyText, itemWidth, previewValue);
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

    private static void ConsumePendingResult<T>(string comboID, IList<T> items, ref int selectedIndex, Action<T>? onDelete)
    {
        if (!SelectorTasks.TryGetValue(comboID, out var state) || state.Task == null || !state.Task.IsCompleted)
            return;

        var result = state.Task.GetAwaiter().GetResult();
        state.Task = null;

        if (result.Kind == CollectionSelectorResultKind.Cancelled)
        {
            CleanupState(comboID, state);
            return;
        }

        if (result.Index < 0 || result.Index >= items.Count)
        {
            selectedIndex = NormalizeSelectedIndex(selectedIndex, items.Count);
            CleanupState(comboID, state);
            return;
        }

        switch (result.Kind)
        {
            case CollectionSelectorResultKind.Selected:
                selectedIndex = result.Index;
                break;

            case CollectionSelectorResultKind.DeleteRequested when onDelete != null:
                onDelete(items[result.Index]);
                selectedIndex = NormalizeSelectedIndex(selectedIndex, items.Count);
                break;
        }

        CleanupState(comboID, state);
    }

    private static void DrawSelectorCombo<T>
    (
        string          label,
        string          comboID,
        IList<T>        items,
        int             selectedIndex,
        Func<T, string> getName,
        bool            allowDelete,
        string          emptyText,
        float           itemWidth,
        string          previewValue
    )
    {
        ImGui.SetNextItemWidth(itemWidth <= 0f ? itemWidth : itemWidth * GlobalUIScale);
        using var combo = ImRaii.Combo(comboID, previewValue, ImGuiComboFlags.HeightLarge);
        if (combo)
            ImGui.CloseCurrentPopup();

        if (!ImGui.IsItemClicked())
            return;

        var request = new CollectionSelectorRequest
        (
            BuildWindowTitle(label),
            emptyText,
            selectedIndex,
            items.Select(item => new CollectionSelectorItem(getName(item))).ToArray(),
            allowDelete
        );

        var state = SelectorTasks.GetValueOrDefault(comboID);

        if (state == null)
        {
            state                  = new();
            SelectorTasks[comboID] = state;
        }

        state.Title = request.Title;
        state.Task  = CollectionSelectorWindow.OpenAsync(request);
    }

    private static string BuildWindowTitle(string label)
    {
        var trimmed = label.Trim().TrimEnd(':', '：');
        if (string.IsNullOrWhiteSpace(trimmed))
            return "选择项目";

        return trimmed.StartsWith("选择", StringComparison.Ordinal) ? trimmed : $"选择{trimmed}";
    }

    private static void CleanupState(string comboID, SelectorTaskState state)
    {
        if (state.Task != null)
            return;

        SelectorTasks.Remove(comboID);
    }

    private sealed class SelectorTaskState
    {
        public Task<CollectionSelectorResult>? Task { get; set; }

        public string Title { get; set; } = string.Empty;
    }
}
