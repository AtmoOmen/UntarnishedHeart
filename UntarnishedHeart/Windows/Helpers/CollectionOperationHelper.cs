using UntarnishedHeart.Execution.Enums;

namespace UntarnishedHeart.Windows.Helpers;

internal static class CollectionOperationHelper
{
    public static int Apply<T>
    (
        List<T>           items,
        int               index,
        StepOperationType operation,
        int               selectedIndex       = -1,
        Func<T>?          createNew           = null,
        Func<T>?          createClipboardCopy = null,
        Func<T>?          createCurrentCopy   = null
    )
    {
        switch (operation)
        {
            case StepOperationType.Delete:
                items.RemoveAt(index);
                return NormalizeSelectedIndexAfterDelete(selectedIndex, index, items.Count);

            case StepOperationType.MoveDown:
                if (index >= items.Count - 1)
                    return selectedIndex;

                items.Swap(index, index                              + 1);
                return MoveSelectedIndex(selectedIndex, index, index + 1);

            case StepOperationType.MoveUp:
                if (index <= 0)
                    return selectedIndex;

                items.Swap(index, index                              - 1);
                return MoveSelectedIndex(selectedIndex, index, index - 1);

            case StepOperationType.Paste:
                if (createClipboardCopy == null)
                    return selectedIndex;

                items[index] = createClipboardCopy();
                return selectedIndex >= 0 ? index : selectedIndex;

            case StepOperationType.PasteUp:
                if (createClipboardCopy == null)
                    return selectedIndex;

                items.Insert(index, createClipboardCopy());
                return selectedIndex >= 0 ? index : selectedIndex;

            case StepOperationType.PasteDown:
                if (createClipboardCopy == null)
                    return selectedIndex;

                items.Insert(index + 1, createClipboardCopy());
                return selectedIndex >= 0 ? index + 1 : selectedIndex;

            case StepOperationType.PasteCurrent:
                if (createCurrentCopy == null)
                    return selectedIndex;

                items.Insert(index, createCurrentCopy());
                return selectedIndex >= 0 ? index : selectedIndex;

            case StepOperationType.InsertUp:
                if (createNew == null)
                    return selectedIndex;

                items.Insert(index, createNew());
                return selectedIndex >= 0 ? index : selectedIndex;

            case StepOperationType.InsertDown:
                if (createNew == null)
                    return selectedIndex;

                items.Insert(index + 1, createNew());
                return selectedIndex >= 0 ? index + 1 : selectedIndex;

            case StepOperationType.Pass:
            default:
                return selectedIndex;
        }
    }

    private static int NormalizeSelectedIndexAfterDelete(int selectedIndex, int removedIndex, int count)
    {
        if (selectedIndex < 0)
            return -1;

        if (count == 0)
            return -1;

        if (selectedIndex == removedIndex)
            return Math.Min(removedIndex, count - 1);

        if (selectedIndex > removedIndex)
            return selectedIndex - 1;

        return selectedIndex;
    }

    private static int MoveSelectedIndex(int selectedIndex, int sourceIndex, int targetIndex)
    {
        if (selectedIndex < 0)
            return -1;

        if (selectedIndex == sourceIndex)
            return targetIndex;

        if (selectedIndex == targetIndex)
            return sourceIndex;

        return selectedIndex;
    }
}
