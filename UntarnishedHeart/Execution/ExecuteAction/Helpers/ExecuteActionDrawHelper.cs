using System.Numerics;
using OmenTools.OmenService;
using UntarnishedHeart.Execution.Condition;
using UntarnishedHeart.Execution.Models;
using UntarnishedHeart.Execution.Preset.Enums;
using UntarnishedHeart.Execution.Preset.Helpers;

namespace UntarnishedHeart.Execution.ExecuteAction.Helpers;

internal static class ExecuteActionDrawHelper
{
    public static void DrawActionReference(ActionReference reference, string idSuffix = "")
    {
        ImGui.SetNextItemWidth(240f * GlobalUIScale);
        reference.ActionType = ConditionBase.DrawEnumCombo($"技能类型###ActionTypeCombo{idSuffix}", reference.ActionType);

        var actionID = reference.ActionID;
        ImGui.SetNextItemWidth(240f * GlobalUIScale);
        if (ImGui.InputUInt($"技能 ID###{idSuffix}", ref actionID))
            reference.ActionID = actionID;
    }

    public static void DrawTargetSelector(TargetSelector selector, string idSuffix)
    {
        ImGui.SetNextItemWidth(240f * GlobalUIScale);
        selector.Kind = ConditionBase.DrawEnumCombo($"目标选择方式###TargetSelectorKind{idSuffix}", selector.Kind);

        switch (selector.Kind)
        {
            case TargetSelectorKind.ByObjectKindAndDataID:

                using (ImRaii.Group())
                {
                    ImGui.SetNextItemWidth(240f * GlobalUIScale);
                    selector.ObjectKind = ConditionBase.DrawEnumCombo($"对象类型###TargetObjectKind{idSuffix}", selector.ObjectKind);

                    var dataID = selector.DataID;
                    ImGui.SetNextItemWidth(240f * GlobalUIScale);
                    if (ImGui.InputUInt($"Data ID###{idSuffix}", ref dataID))
                        selector.DataID = dataID;
                }

                ImGui.SameLine();

                if (ImGuiOm.ButtonIconWithTextVertical(FontAwesomeIcon.Bullseye, "选择当前目标"))
                {
                    if (TargetManager.Target is { } target)
                    {
                        selector.ObjectKind = target.ObjectKind;
                        selector.DataID     = target.DataID;
                    }
                }

                var requireTargetable = selector.RequireTargetable;
                if (ImGui.Checkbox($"要求对象为可选中状态###{idSuffix}", ref requireTargetable))
                    selector.RequireTargetable = requireTargetable;
                break;

            case TargetSelectorKind.ByEntityID:
                var entityID = selector.EntityID;
                ImGui.SetNextItemWidth(240f * GlobalUIScale);
                if (ImGui.InputUInt($"Entity ID###{idSuffix}", ref entityID))
                    selector.EntityID = entityID;

                ImGui.SameLine();

                if (ImGuiOm.ButtonIcon("SelectTarget", FontAwesomeIcon.Bullseye, "选择当前目标"))
                {
                    if (TargetManager.Target is { } target)
                        selector.EntityID = target.EntityID;
                }

                break;
        }
    }

    public static void DrawPositionSelector(string buttonID, Action<Vector3> setPosition)
    {
        ImGui.SameLine();
        if (ImGuiOm.ButtonIcon(buttonID, FontAwesomeIcon.Bullseye, "取当前位置", true) &&
            DService.Instance().ObjectTable.LocalPlayer is { } localPlayer)
            setPosition(localPlayer.Position);
    }

    public static void DrawNoExtraParametersHint() =>
        ImGui.TextDisabled("此动作无需额外参数");
}
