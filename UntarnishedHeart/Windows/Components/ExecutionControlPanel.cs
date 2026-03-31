using Dalamud.Interface.Colors;

namespace UntarnishedHeart.Windows.Components;

internal static class ExecutionControlPanel
{
    public static void DrawStatus(string statusLabel, bool isRunning, string progressLabel, string progressText, string runningMessage)
    {
        ImGui.TextColored(KnownColor.LightSkyBlue.ToVector4(), "运行状态:");
        using var indent = ImRaii.PushIndent();

        ImGui.Text("当前状态:");
        ImGui.SameLine();
        ImGui.TextColored
        (
            isRunning ? ImGuiColors.ParsedGreen : ImGuiColors.DalamudRed,
            isRunning ? "运行中" : "等待中"
        );

        ImGui.Text(progressLabel);
        ImGui.SameLine();
        ImGui.Text(progressText);

        ImGui.Text(statusLabel);
        ImGui.SameLine();
        ImGui.TextWrapped(runningMessage);
    }

    public static void DrawControls(string startLabel, Action onStart, bool canStart, string stopLabel, Action onStop)
    {
        using (ImRaii.Disabled(!canStart))
        {
            if (ImGuiOm.ButtonSelectable(startLabel))
                onStart();
        }

        if (ImGuiOm.ButtonSelectable(stopLabel))
            onStop();
    }
}
