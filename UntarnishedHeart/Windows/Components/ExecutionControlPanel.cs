using UntarnishedHeart.Windows.Helpers;

namespace UntarnishedHeart.Windows.Components;

internal static class ExecutionControlPanel
{
    public static void DrawStatus
    (
        ExecutionStatusViewState status
    )
    {
        ImGui.TextDisabled(status.ModeName);
        ImGui.TextColored
        (
            status.IsRunning ?
                KnownColor.LimeGreen.ToVector4() :
                KnownColor.IndianRed.ToVector4(),
            status.IsRunning ?
                "执行中" :
                "待命"
        );
        ImGui.SameLine();
        ImGui.TextDisabled($"{status.ProgressLabel} {status.ProgressText}");

        ImGui.Spacing();

        if (string.IsNullOrWhiteSpace(status.RunningMessage))
            ImGui.TextDisabled("暂无运行信息");
        else
            ImGui.TextWrapped(status.RunningMessage);

        using (ImRaii.Disabled(!ExecutionUIHelper.CanNavigate()))
        {
            if (ImGui.Button("跳转", new(-1f, 0f)))
            {
                var targets = ExecutionUIHelper.GetNavigationTargets();

                if (targets.Count > 0)
                {
                    CollectionSelectorWindow.Open
                    (
                        "跳转到步骤或动作",
                        "当前无可跳转目标",
                        -1,
                        targets,
                        static target => target.Label,
                        index =>
                        {
                            if ((uint)index >= (uint)targets.Count)
                                return;

                            ExecutionUIHelper.NavigateTo(targets[index]);
                        }
                    );
                }
            }
        }

        if (ImGui.IsItemHovered())
            ImGuiOm.TooltipHover("跳转到任意步骤或动作");

        ImGui.NewLine();

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
