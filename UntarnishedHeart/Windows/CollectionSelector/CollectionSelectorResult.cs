namespace UntarnishedHeart.Windows;

internal readonly record struct CollectionSelectorResult
(
    CollectionSelectorResultKind Kind,
    int                          Index
)
{
    public static CollectionSelectorResult Selected(int index) => new(CollectionSelectorResultKind.Selected, index);

    public static CollectionSelectorResult DeleteRequested(int index) => new(CollectionSelectorResultKind.DeleteRequested, index);

    public static CollectionSelectorResult Cancelled() => new(CollectionSelectorResultKind.Cancelled, -1);
}
