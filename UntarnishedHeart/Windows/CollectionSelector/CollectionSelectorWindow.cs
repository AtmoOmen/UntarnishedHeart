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

    internal static void Open
    (
        CollectionSelectorRequest request,
        Action<int>               onSelected,
        Action<int>?              onDelete = null,
        Action?                   onCancel = null
    )
    {
        var window = WindowManager.Instance().Get<CollectionSelectorWindow>() ?? throw new InvalidOperationException("集合选择窗口尚未注册");
        window.OpenInternal(request, onSelected, onDelete, onCancel);
    }

    private void OpenInternal
    (
        CollectionSelectorRequest request,
        Action<int>               selectedCallback,
        Action<int>?              deleteCallback,
        Action?                   cancelCallback
    )
    {
        CancelPendingRequest();

        currentRequest = NormalizeRequest(request);

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
        var request = NormalizeCurrentRequest();

        if (request == null || onSelected == null)
        {
            IsOpen = false;
            return;
        }

        ImGui.Text(request.Title);
        ImGui.Spacing();

        if (focusSearchOnOpen)
        {
            ImGui.SetKeyboardFocusHere();
            focusSearchOnOpen = false;
        }

        ImGui.SetNextItemWidth(-1f);
        ImGui.InputTextWithHint("###CollectionSelectorSearch", "输入关键字筛选", ref searchText, 256);

        var filteredIndices     = BuildFilteredIndices(request, searchText);
        var hasVisibleSelection = NormalizeVisibleSelection(filteredIndices);

        ImGui.Spacing();

        var footerHeight = ImGui.GetFrameHeightWithSpacing() + ImGui.GetStyle().WindowPadding.Y;

        using (var child = ImRaii.Child("CollectionSelectorItemsChild", new Vector2(0f, -footerHeight), true))
        {
            if (child)
                DrawItemList(request, filteredIndices);
        }

        HandleKeyboard(request, filteredIndices, hasVisibleSelection);

        if (ImGui.Button("取消", new Vector2(-1f, 0f))) CancelPendingRequest();
    }

    public override void OnClose() => CancelPendingRequest();

    private void DrawItemList(CollectionSelectorRequest request, List<int> filteredIndices)
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
            if (!IsValidIndex(index, request.Items.Count))
                continue;

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

            highlightedIndex    = index;
            scrollToHighlighted = false;

            using var deleteDisabled = ImRaii.Disabled(request.Items.Count <= 1);

            if (ImGui.MenuItem($"删除##CollectionSelectorDelete-{index}"))
            {
                CompleteDelete(index);
                return;
            }
        }
    }

    private void HandleKeyboard(CollectionSelectorRequest request, List<int> filteredIndices, bool hasVisibleSelection)
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
            var currentFilteredIndex = FindIndex(filteredIndices, highlightedIndex);

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
            CompleteSelection(NormalizeVisibleIndex(request, highlightedIndex, filteredIndices));
    }

    private static List<int> BuildFilteredIndices(CollectionSelectorRequest request, string searchText)
    {
        var indices = new List<int>(request.Items.Count);
        var keyword = searchText.Trim();

        for (var i = 0; i < request.Items.Count; i++)
        {
            var item = request.Items[i];

            if (string.IsNullOrWhiteSpace(keyword))
            {
                indices.Add(i);
                continue;
            }

            var textMatched = !string.IsNullOrWhiteSpace(item.Text) &&
                              item.Text.Contains(keyword, StringComparison.OrdinalIgnoreCase);
            var descriptionMatched = !string.IsNullOrWhiteSpace(item.Description) &&
                                     item.Description.Contains(keyword, StringComparison.OrdinalIgnoreCase);

            if (textMatched || descriptionMatched)
                indices.Add(i);
        }

        return indices;
    }

    private void CompleteSelection(int index)
    {
        var request  = currentRequest;
        var callback = onSelected;
        if (request == null || callback == null)
            return;

        var normalizedIndex = NormalizeItemIndex(request.Items, index);
        if (normalizedIndex < 0)
            return;

        ResetState();
        callback(normalizedIndex);
    }

    private void CompleteDelete(int index)
    {
        var request  = currentRequest;
        var callback = onDelete;

        if (request == null)
            return;

        if (callback == null)
        {
            CancelPendingRequest();
            return;
        }

        var normalizedIndex = NormalizeItemIndex(request.Items, index);
        if (normalizedIndex < 0 || request.Items.Count <= 1)
            return;

        ResetState();
        callback(normalizedIndex);
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

    private CollectionSelectorRequest? NormalizeCurrentRequest()
    {
        var request = currentRequest;
        if (request == null)
            return null;

        var normalizedRequest = NormalizeRequest(request);
        if (!ReferenceEquals(normalizedRequest, request))
            currentRequest = normalizedRequest;

        highlightedIndex = NormalizeItemIndex(normalizedRequest.Items, highlightedIndex);
        if (highlightedIndex < 0)
            scrollToHighlighted = false;

        return normalizedRequest;
    }

    private bool NormalizeVisibleSelection(List<int> filteredIndices)
    {
        if (filteredIndices.Count == 0)
        {
            highlightedIndex    = -1;
            scrollToHighlighted = false;
            return false;
        }

        if (FindIndex(filteredIndices, highlightedIndex) >= 0)
            return true;

        highlightedIndex = filteredIndices[0];
        return true;
    }

    private static CollectionSelectorRequest NormalizeRequest(CollectionSelectorRequest request)
    {
        var normalizedIndex = NormalizeItemIndex(request.Items, request.SelectedIndex);
        return normalizedIndex == request.SelectedIndex
                   ? request
                   : request with { SelectedIndex = normalizedIndex };
    }

    private static int NormalizeVisibleIndex(CollectionSelectorRequest request, int index, IReadOnlyList<int> filteredIndices)
    {
        var normalizedIndex = NormalizeItemIndex(request.Items, index);
        if (normalizedIndex < 0)
            return -1;

        return FindIndex(filteredIndices, normalizedIndex) >= 0 ? normalizedIndex : -1;
    }

    private static int NormalizeItemIndex(IReadOnlyList<CollectionSelectorItem> items, int index) => 
        NormalizeIndex(index, items.Count);

    private static int NormalizeIndex(int index, int count) => 
        count <= 0 ? -1 : Math.Clamp(index, 0, count - 1);

    private static bool IsValidIndex(int index, int count) => 
        (uint)index < (uint)count;

    private static int FindIndex(IReadOnlyList<int> indices, int value)
    {
        for (var i = 0; i < indices.Count; i++)
            if (indices[i] == value)
                return i;

        return -1;
    }
}
