using Dalamud.Interface.Windowing;
using UntarnishedHeart.Windows.Components;

namespace UntarnishedHeart.Windows;

internal abstract class CollectionEditorWindowBase<TItem>
(
    string           title,
    ImGuiWindowFlags flags = ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse
) : Window(title, flags)
{
    private int selectedIndex = -1;

    protected abstract string CollectionID { get; }

    protected abstract string SelectorLabel { get; }

    protected abstract string EmptyCollectionText { get; }

    protected abstract string EmptySelectionText { get; }

    protected abstract IList<TItem> Items { get; }

    protected abstract string GetItemName(TItem item);

    protected abstract TItem CreateNewItem();

    protected abstract TItem? ImportItem();

    protected abstract void ExportItem(TItem item);

    protected abstract void SaveItems();

    protected abstract void DrawEditor(TItem item);

    protected virtual void OnItemAdded(TItem item, bool imported)
    {
        if (imported)
            SaveItems();
    }

    public override void Draw()
    {
        DrawToolbar();

        ImGui.Separator();
        ImGui.Spacing();

        selectedIndex = CollectionToolbar.NormalizeSelectedIndex(selectedIndex, Items.Count);

        if (selectedIndex < 0)
        {
            ImGui.TextDisabled(EmptySelectionText);
            return;
        }

        DrawEditor(Items[selectedIndex]);
    }

    private void DrawToolbar()
    {
        selectedIndex = CollectionToolbar.NormalizeSelectedIndex(selectedIndex, Items.Count);

        if (!string.IsNullOrEmpty(SelectorLabel))
        {
            ImGui.AlignTextToFramePadding();
            ImGui.Text(SelectorLabel);
            ImGui.SameLine();
        }

        if (Items.Count == 0)
            ImGui.TextDisabled(EmptyCollectionText);
        else
        {
            var previewValue = selectedIndex >= 0 ? GetItemName(Items[selectedIndex]) : "请选择";

            ImGui.SetNextItemWidth(280f * GlobalUIScale);

            using (var combo = ImRaii.Combo($"###{CollectionID}SelectCombo", previewValue, ImGuiComboFlags.HeightLarge))
            {
                if (combo)
                    ImGui.CloseCurrentPopup();
            }

            if (ImGui.IsItemClicked())
            {
                var trimmed = SelectorLabel.Trim().TrimEnd(':', '：');
                var title = string.IsNullOrWhiteSpace(trimmed)
                                ? "选择项目"
                                : trimmed.StartsWith("选择", StringComparison.Ordinal)
                                    ? trimmed
                                    : $"选择{trimmed}";

                CollectionSelectorWindow.Open
                (
                    title,
                    EmptyCollectionText,
                    selectedIndex,
                    Items,
                    GetItemName,
                    index =>
                    {
                        if ((uint)index >= (uint)Items.Count)
                            return;

                        selectedIndex = index;
                    },
                    index =>
                    {
                        if ((uint)index >= (uint)Items.Count)
                            return;

                        Items.RemoveAt(index);
                        selectedIndex = CollectionToolbar.NormalizeSelectedIndex(selectedIndex, Items.Count);
                    }
                );
            }
        }

        ImGui.SameLine();

        CollectionToolbar.DrawActionButtons
        (
            $"Save{CollectionID}",
            SaveItems,
            $"AddNew{CollectionID}",
            AddItem,
            $"Import{CollectionID}",
            ImportItemFromClipboard,
            $"Export{CollectionID}",
            ExportSelectedItem,
            Items.Count > 0
        );
    }

    private void AddItem()
    {
        var item = CreateNewItem();
        Items.Add(item);
        selectedIndex = Items.Count - 1;
        OnItemAdded(item, false);
    }

    private void ImportItemFromClipboard()
    {
        var item = ImportItem();
        if (item == null)
            return;

        Items.Add(item);
        selectedIndex = Items.Count - 1;
        OnItemAdded(item, true);
    }

    private void ExportSelectedItem()
    {
        selectedIndex = CollectionToolbar.NormalizeSelectedIndex(selectedIndex, Items.Count);
        if (selectedIndex < 0)
            return;

        ExportItem(Items[selectedIndex]);
    }
}
