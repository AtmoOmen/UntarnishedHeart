using UntarnishedHeart.Execution.Preset;
using UntarnishedHeart.Internal;
using UntarnishedHeart.Windows.Components;

namespace UntarnishedHeart.Windows;

internal class PresetEditor() : CollectionEditorWindowBase<Preset>($"预设编辑器###{Plugin.PLUGIN_NAME}-PresetEditor")
{
    protected override string CollectionID => "Preset";

    protected override string SelectorLabel => "选择预设:";

    protected override string EmptyCollectionText => "暂无预设";

    protected override string EmptySelectionText => "暂无预设，请先添加或导入";

    protected override IList<Preset> Items => PluginConfig.Instance().Presets;

    protected override string GetItemName(Preset item) => item.Name;

    protected override Preset CreateNewItem() => new();

    protected override Preset? ImportItem() => Preset.ImportFromClipboard();

    protected override void ExportItem(Preset item) => item.ExportToClipboard();

    protected override void SaveItems() => PluginConfig.Instance().Save();

    protected override void DrawEditor(Preset item) => PresetEditorPanel.Draw(item);
}
