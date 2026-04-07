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
        ConditionBase.DrawLabel("技能类型", KnownColor.LightSkyBlue.ToVector4());
        reference.ActionType = ConditionBase.DrawEnumCombo("###ActionTypeCombo", reference.ActionType);

        ConditionBase.DrawLabel("技能 ID", KnownColor.LightSkyBlue.ToVector4());
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
        ConditionBase.DrawLabel("选择方式", KnownColor.LightSkyBlue.ToVector4());
        selector.Kind = ConditionBase.DrawEnumCombo($"###TargetSelectorKind{idSuffix}", selector.Kind);

        switch (selector.Kind)
        {
            case TargetSelectorKind.ByObjectKindAndDataID:
                ConditionBase.DrawLabel("对象类型", KnownColor.LightSkyBlue.ToVector4());
                
                ImGui.SetNextItemWidth(240f * GlobalUIScale);
                selector.ObjectKind = ConditionBase.DrawEnumCombo($"###TargetObjectKind{idSuffix}", selector.ObjectKind);

                ConditionBase.DrawLabel("Data ID", KnownColor.LightSkyBlue.ToVector4());
                
                var dataID = selector.DataID;
                ImGui.SetNextItemWidth(240f * GlobalUIScale);
                if (ImGui.InputUInt($"###TargetDataId{idSuffix}", ref dataID))
                    selector.DataID = dataID;

                ConditionBase.DrawLabel("要求可选中", KnownColor.LightSkyBlue.ToVector4());
                
                var requireTargetable = selector.RequireTargetable;
                if (ImGui.Checkbox($"###RequireTargetable{idSuffix}", ref requireTargetable))
                    selector.RequireTargetable = requireTargetable;
                break;

            case TargetSelectorKind.ByEntityID:
                ConditionBase.DrawLabel("Entity ID", KnownColor.LightSkyBlue.ToVector4());
                
                var entityID = selector.EntityID;
                ImGui.SetNextItemWidth(240f * GlobalUIScale);
                if (ImGui.InputUInt($"###TargetEntityId{idSuffix}", ref entityID))
                    selector.EntityID = entityID;
                break;
        }
    }

    public static void DrawPositionRange(PositionRange range)
    {
        ConditionBase.DrawLabel("中心点", KnownColor.LightSkyBlue.ToVector4());
        
        var center = range.Center;
        ImGui.SetNextItemWidth(240f * GlobalUIScale);
        if (ImGui.InputFloat3("###PositionRangeCenter", ref center))
            range.Center = center;

        ConditionBase.DrawLabel("半径", KnownColor.LightSkyBlue.ToVector4());
        var radius = range.Radius;
        ImGui.SetNextItemWidth(240f * GlobalUIScale);
        if (ImGui.InputFloat("###PositionRangeRadius", ref radius))
            range.Radius = Math.Max(0f, radius);
    }
}
