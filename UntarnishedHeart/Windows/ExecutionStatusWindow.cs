using Dalamud.Interface.Windowing;
using UntarnishedHeart.Windows.Components;
using UntarnishedHeart.Windows.Helpers;

namespace UntarnishedHeart.Windows;

public class ExecutionStatusWindow() : Window($"运行状态###{Plugin.PLUGIN_NAME}-ExecutionStatusWindow", ImGuiWindowFlags.AlwaysAutoResize)
{
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
