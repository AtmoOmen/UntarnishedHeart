using System.Numerics;
using Dalamud.Interface.Windowing;
using OmenTools.OmenService;

namespace UntarnishedHeart.Windows;

internal class CollectionSelectorWindow : Window
{
    private CollectionSelectorRequest? currentRequest;
    private Action<int>?               onSelected;
    private Action<int>?               onDelete;
    private Action?                    onCancel;

    private string searchText       = string.Empty;
    private int    highlightedIndex = -1;
    private bool   focusSearchOnOpen;
    private bool   scrollToHighlighted;

    public CollectionSelectorWindow() : base($"集合选择###{Plugin.PLUGIN_NAME}-CollectionSelectorWindow")
    {
        Flags = ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoTitleBar;
        SizeConstraints = new()
        {
            MinimumSize = new(460f, 360f)
        };
    }

    internal static void Open(CollectionSelectorRequest request, Action<int> onSelected, Action<int>? onDelete = null, Action? onCancel = null)
    {
        var window = WindowManager.Instance().Get<CollectionSelectorWindow>() ?? throw new InvalidOperationException("集合选择窗口尚未注册");
        window.OpenInternal(request, onSelected, onDelete, onCancel);
    }

    private void OpenInternal(CollectionSelectorRequest request, Action<int> selectedCallback, Action<int>? deleteCallback, Action? cancelCallback)
    {
        CancelPendingRequest();

        currentRequest = request with
        {
            SelectedIndex = Math.Clamp(request.SelectedIndex, request.Items.Count > 0 ? 0 : -1, request.Items.Count - 1)
        };

        onSelected          = selectedCallback;
        onDelete            = deleteCallback;
        onCancel            = cancelCallback;
        searchText          = string.Empty;
        highlightedIndex    = currentRequest.SelectedIndex;
        focusSearchOnOpen   = true;
        scrollToHighlighted = true;
        IsOpen              = true;
    }

    public override void Draw()
    {
        if (currentRequest == null || onSelected == null)
        {
            IsOpen = false;
            return;
        }

        ImGui.Text(currentRequest.Title);
        ImGui.Spacing();

        if (focusSearchOnOpen)
        {
            ImGui.SetKeyboardFocusHere();
            focusSearchOnOpen = false;
        }

        ImGui.SetNextItemWidth(-1f);
        ImGui.InputTextWithHint("###CollectionSelectorSearch", "输入关键字筛选", ref searchText, 256);

        var filteredIndices     = BuildFilteredIndices(currentRequest, searchText);
        var hasVisibleSelection = filteredIndices.Contains(highlightedIndex);

        if (!hasVisibleSelection && filteredIndices.Count > 0)
        {
            highlightedIndex    = filteredIndices[0];
            hasVisibleSelection = true;
        }

        ImGui.Spacing();

        var footerHeight = ImGui.GetFrameHeightWithSpacing() + ImGui.GetStyle().WindowPadding.Y;

        using (var child = ImRaii.Child("CollectionSelectorItemsChild", new Vector2(0f, -footerHeight), true))
        {
            if (child)
                DrawItemList(currentRequest, filteredIndices);
        }

        HandleKeyboard(filteredIndices, hasVisibleSelection);

        if (ImGui.Button("取消", new Vector2(-1f, 0f))) CancelPendingRequest();
    }

    public override void OnClose() => CancelPendingRequest();

    private void DrawItemList(CollectionSelectorRequest request, IReadOnlyList<int> filteredIndices)
    {
        if (filteredIndices.Count == 0)
        {
            ImGui.TextDisabled(string.IsNullOrWhiteSpace(searchText) ? request.EmptyText : "未找到匹配项");
            return;
        }

        using var table = ImRaii.Table
        (
            "CollectionSelectorItemsTable",
            1,
            ImGuiTableFlags.RowBg             |
            ImGuiTableFlags.Borders           |
            ImGuiTableFlags.SizingStretchProp |
            ImGuiTableFlags.ScrollY
        );
        if (!table)
            return;

        ImGui.TableSetupColumn("项目", ImGuiTableColumnFlags.WidthStretch);

        foreach (var index in filteredIndices)
        {
            ImGui.TableNextRow();
            ImGui.TableSetColumnIndex(0);

            var item       = request.Items[index];
            var isSelected = highlightedIndex == index;

            if (ImGui.Selectable($"{item.Text}###CollectionSelectorItem-{index}", isSelected, ImGuiSelectableFlags.SpanAllColumns))
            {
                highlightedIndex = index;
                CompleteSelection(index);
                return;
            }

            if (!string.IsNullOrWhiteSpace(item.Description) && ImGui.IsItemHovered())
                ImGuiOm.TooltipHover(item.Description);

            if (scrollToHighlighted && isSelected)
            {
                ImGui.SetScrollHereY();
                scrollToHighlighted = false;
            }

            if (!request.AllowDelete)
                continue;

            using var context = ImRaii.ContextPopupItem($"CollectionSelectorDeletePopup-{index}");
            if (!context)
                continue;

            highlightedIndex = index;

            using var deleteDisabled = ImRaii.Disabled(request.Items.Count <= 1);

            if (ImGui.MenuItem($"删除##CollectionSelectorDelete-{index}"))
            {
                CompleteDelete(index);
                return;
            }
        }
    }

    private void HandleKeyboard(List<int> filteredIndices, bool hasVisibleSelection)
    {
        if (!ImGui.IsWindowFocused(ImGuiFocusedFlags.RootAndChildWindows))
            return;

        if (ImGui.IsKeyPressed(ImGuiKey.Escape))
        {
            CancelPendingRequest();
            return;
        }

        if (filteredIndices.Count > 0)
        {
            var currentFilteredIndex = filteredIndices.IndexOf(highlightedIndex);

            if (ImGui.IsKeyPressed(ImGuiKey.DownArrow))
            {
                var nextIndex = currentFilteredIndex < 0
                                    ? 0
                                    : Math.Min(currentFilteredIndex + 1, filteredIndices.Count - 1);
                highlightedIndex    = filteredIndices[nextIndex];
                scrollToHighlighted = true;
            }
            else if (ImGui.IsKeyPressed(ImGuiKey.UpArrow))
            {
                var nextIndex = currentFilteredIndex <= 0
                                    ? 0
                                    : currentFilteredIndex - 1;
                highlightedIndex    = filteredIndices[nextIndex];
                scrollToHighlighted = true;
            }
        }

        if (hasVisibleSelection && ImGui.IsKeyPressed(ImGuiKey.Enter))
            CompleteSelection(highlightedIndex);
    }

    private static List<int> BuildFilteredIndices(CollectionSelectorRequest request, string searchText)
    {
        var indices = new List<int>(request.Items.Count);

        for (var i = 0; i < request.Items.Count; i++)
        {
            var item = request.Items[i];

            if (string.IsNullOrWhiteSpace(searchText))
            {
                indices.Add(i);
                continue;
            }

            if (!string.IsNullOrWhiteSpace(item.Text) &&
                item.Text.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                !string.IsNullOrWhiteSpace(item.Description) &&
                item.Description.Contains(searchText, StringComparison.OrdinalIgnoreCase))
                indices.Add(i);
        }

        return indices;
    }

    private void CompleteSelection(int index)
    {
        var callback = onSelected;
        if (callback == null)
            return;

        ResetState();
        callback(index);
    }

    private void CompleteDelete(int index)
    {
        var callback = onDelete;

        if (callback == null)
        {
            CancelPendingRequest();
            return;
        }

        ResetState();
        callback(index);
    }

    private void CancelPendingRequest()
    {
        var callback = onCancel;
        if (currentRequest == null && callback == null)
            return;

        ResetState();
        callback?.Invoke();
    }

    private void ResetState()
    {
        currentRequest      = null;
        onSelected          = null;
        onDelete            = null;
        onCancel            = null;
        searchText          = string.Empty;
        highlightedIndex    = -1;
        focusSearchOnOpen   = false;
        scrollToHighlighted = false;
        IsOpen              = false;
    }
}
