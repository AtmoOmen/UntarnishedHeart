using Dalamud.Interface.Colors;
using UntarnishedHeart.Windows.Helpers;

namespace UntarnishedHeart.Windows.Components;

internal static class ExecutionControlPanel
{
    public static void DrawStatus(ExecutionStatusViewState status)
    {
        ImGui.TextDisabled(status.ModeName);
        ImGui.TextColored(status.IsRunning ? ImGuiColors.ParsedGreen : ImGuiColors.DalamudRed, status.IsRunning ? "执行中" : "待命");
        ImGui.SameLine();
        ImGui.TextDisabled($"{status.ProgressLabel} {status.ProgressText}");

        ImGui.Spacing();

        if (string.IsNullOrWhiteSpace(status.RunningMessage))
            ImGui.TextDisabled("暂无运行信息");
        else
            ImGui.TextWrapped(status.RunningMessage);

        ImGui.Spacing();

        using (ImRaii.Disabled(!status.CanStop))
        {
            if (ImGui.Button(status.StopLabel, new(-1f, 0f)))
                status.StopAction();
        }

        using (ImRaii.Disabled(!status.CanDeferredStop))
        {
            if (ImGui.Button(status.DeferredStopLabel, new(-1f, 0f)))
                status.DeferredStopAction();
        }
    }
}
