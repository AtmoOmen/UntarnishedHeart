using UntarnishedHeart.Execution.Route;
using UntarnishedHeart.Internal;
using UntarnishedHeart.Windows.Components;

namespace UntarnishedHeart.Windows;

internal class RouteEditor() : CollectionEditorWindowBase<Route>($"路线编辑器###{Plugin.PLUGIN_NAME}-RouteEditor")
{
    protected override string CollectionID => "Route";

    protected override string SelectorLabel => "选择路线:";

    protected override string EmptyCollectionText => "暂无路线";

    protected override string EmptySelectionText => "请选择一条路线进行编辑";

    protected override IList<Route> Items => PluginConfig.Instance().Routes;

    protected override string GetItemName(Route item) => item.Name;

    protected override Route CreateNewItem() => new() { Name = $"新路线 {Items.Count + 1}" };

    protected override Route? ImportItem() => Route.ImportFromClipboard();

    protected override void ExportItem(Route item) => item.ExportToClipboard();

    protected override void SaveItems() => PluginConfig.Instance().Save();

    protected override void DrawEditor(Route item) => RouteEditorPanel.Draw(item);
}
