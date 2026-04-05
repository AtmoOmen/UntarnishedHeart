using FFXIVClientStructs.FFXIV.Client.Game;
using OmenTools.Interop.Game.Lumina;
using UntarnishedHeart.Execution.CommandCondition.Enums;
using Action = Lumina.Excel.Sheets.Action;

namespace UntarnishedHeart.Execution.CommandCondition.Implementations;

public sealed class ActionCastStartCommandCondition : CommandSingleCondition
{
    public override CommandDetectType Kind => CommandDetectType.ActionCastStart;

    public uint ActionID { get; set; }

    public override bool Evaluate(in CommandConditionContext context) =>
        context.Target is { IsCasting: true, CastActionType: ActionType.Action } target &&
        target.CastActionID == ActionID;

    public override CommandSingleCondition DeepCopy() =>
        new ActionCastStartCommandCondition
        {
            ActionID = ActionID
        };

    protected override void DrawBody()
    {
        DrawLabel("目标类型", KnownColor.LightSkyBlue.ToVector4());
        ImGui.TextUnformatted("目标");

        DrawLabel("比较类型", KnownColor.LightSkyBlue.ToVector4());
        ImGui.TextUnformatted("拥有");

        DrawLabel("技能 ID", KnownColor.LightSkyBlue.ToVector4());
        var actionID = (int)ActionID;
        if (ImGui.InputInt("###ActionIdInput", ref actionID))
            ActionID = (uint)Math.Max(0, actionID);
        
        if (LuminaGetter.TryGetRow((uint)actionID, out Action row))
        {
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.TableNextColumn();
            
            ImGui.TextUnformatted($"({row.Name})");
        }
    }

    protected override string Describe() =>
        $"技能咏唱开始 目标 拥有 {ActionID}";
}
