using FFXIVClientStructs.FFXIV.Client.Game;
using OmenTools.Interop.Game.Lumina;
using UntarnishedHeart.Execution.Models;
using UntarnishedHeart.Execution.Preset.Enums;
using UntarnishedHeart.Execution.Preset.Helpers;
using Action = Lumina.Excel.Sheets.Action;

namespace UntarnishedHeart.Execution.Condition.Helpers;

internal static class ConditionDrawHelper
{
    public static void DrawActionReference(ActionReference reference)
    {
        Condition.DrawLabel("技能类型", KnownColor.LightSkyBlue.ToVector4());
        reference.ActionType = Condition.DrawEnumCombo("###ActionTypeCombo", reference.ActionType);

        Condition.DrawLabel("技能 ID", KnownColor.LightSkyBlue.ToVector4());
        var actionID = reference.ActionID;
        if (ImGui.InputUInt("###ActionIdInput", ref actionID))
            reference.ActionID = actionID;

        if (reference.ActionType == ActionType.Action && LuminaGetter.TryGetRow(reference.ActionID, out Action actionRow))
        {
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.TableNextColumn();
            ImGui.TextUnformatted($"({actionRow.Name})");
        }
    }

    public static void DrawTargetSelector(TargetSelector selector, string idSuffix = "")
    {
        Condition.DrawLabel("选择方式", KnownColor.LightSkyBlue.ToVector4());
        selector.Kind = Condition.DrawEnumCombo($"###TargetSelectorKind{idSuffix}", selector.Kind);

        switch (selector.Kind)
        {
            case TargetSelectorKind.ByObjectKindAndDataID:
                Condition.DrawLabel("对象类型", KnownColor.LightSkyBlue.ToVector4());
                selector.ObjectKind = Condition.DrawEnumCombo($"###TargetObjectKind{idSuffix}", selector.ObjectKind);

                Condition.DrawLabel("Data ID", KnownColor.LightSkyBlue.ToVector4());
                var dataID = selector.DataID;
                if (ImGui.InputUInt($"###TargetDataId{idSuffix}", ref dataID))
                    selector.DataID = dataID;

                Condition.DrawLabel("要求可选中", KnownColor.LightSkyBlue.ToVector4());
                var requireTargetable = selector.RequireTargetable;
                if (ImGui.Checkbox($"###RequireTargetable{idSuffix}", ref requireTargetable))
                    selector.RequireTargetable = requireTargetable;
                break;

            case TargetSelectorKind.ByEntityID:
                Condition.DrawLabel("Entity ID", KnownColor.LightSkyBlue.ToVector4());
                var entityID = selector.EntityID;
                if (ImGui.InputUInt($"###TargetEntityId{idSuffix}", ref entityID))
                    selector.EntityID = entityID;
                break;
        }
    }

    public static void DrawPositionRange(PositionRange range)
    {
        Condition.DrawLabel("中心点", KnownColor.LightSkyBlue.ToVector4());
        var center = range.Center;
        if (ImGui.InputFloat3("###PositionRangeCenter", ref center))
            range.Center = center;

        Condition.DrawLabel("半径", KnownColor.LightSkyBlue.ToVector4());
        var radius = range.Radius;
        if (ImGui.InputFloat("###PositionRangeRadius", ref radius))
            range.Radius = Math.Max(0f, radius);
    }
}
