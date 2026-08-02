using System.Numerics;
using Dalamud.Interface.Windowing;
using UntarnishedHeart.Windows.Components;
using UntarnishedHeart.Windows.Helpers;

namespace UntarnishedHeart.Windows;

public class ExecutionStatusWindow : Window
{
    public ExecutionStatusWindow() : base
    (
        $"运行状态###{Plugin.PLUGIN_NAME}-ExecutionStatusWindow",
        ImGuiWindowFlags.AlwaysAutoResize
    ) =>
        SizeConstraints = new()
        {
            MinimumSize = new Vector2(200, 150) * GlobalUIScale,
            MaximumSize = new Vector2(200, 150) * GlobalUIScale
        };

    public override void Draw()
    {
        var state = ExecutionUIHelper.CreateStatusViewState();

        if (!state.IsRunning)
        {
            IsOpen = false;
            return;
        }

        ExecutionControlPanel.DrawStatus(state);
    }
}
