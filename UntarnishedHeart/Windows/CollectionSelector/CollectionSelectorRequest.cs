namespace UntarnishedHeart.Windows;

internal sealed record CollectionSelectorRequest
(
    string                                Title,
    string                                EmptyText,
    int                                   SelectedIndex,
    IReadOnlyList<CollectionSelectorItem> Items,
    bool                                  AllowDelete = false
);
