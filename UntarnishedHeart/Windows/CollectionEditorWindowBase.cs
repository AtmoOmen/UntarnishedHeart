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
        CollectionToolbar.DrawSelector
        (
            SelectorLabel,
            $"###{CollectionID}SelectCombo",
            Items,
            selectedIndex,
            value => selectedIndex = value,
            GetItemName,
            item => Items.Remove(item),
            EmptyCollectionText
        );

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
