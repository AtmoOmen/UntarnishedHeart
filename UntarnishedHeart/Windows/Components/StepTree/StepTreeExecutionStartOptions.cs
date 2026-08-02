namespace UntarnishedHeart.Windows.Components;

internal sealed class StepTreeExecutionStartOptions
{
    public bool IsVisible { get; init; }

    public Action<int>? StartFromStep { get; init; }

    public Action<int, int>? StartFromAction { get; init; }
}
