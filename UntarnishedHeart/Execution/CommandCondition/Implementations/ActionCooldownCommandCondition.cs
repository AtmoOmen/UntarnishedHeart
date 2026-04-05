using FFXIVClientStructs.FFXIV.Client.Game;
using OmenTools.Interop.Game.Lumina;
using UntarnishedHeart.Execution.CommandCondition.Enums;
using Action = Lumina.Excel.Sheets.Action;

namespace UntarnishedHeart.Execution.CommandCondition.Implementations;

public sealed class ActionCooldownCommandCondition : CommandSingleCondition
{
    public override CommandDetectType Kind => CommandDetectType.ActionCooldown;

    public CooldownComparisonType ComparisonType { get; set; } = CooldownComparisonType.Finished;

    public uint ActionID { get; set; }

    public override unsafe bool Evaluate(in CommandConditionContext context)
    {
        var isOffCooldown = ActionManager.Instance()->IsActionOffCooldown(ActionType.Action, ActionID);
        return ComparisonType switch
        {
            CooldownComparisonType.Finished    => isOffCooldown,
            CooldownComparisonType.NotFinished => !isOffCooldown,
            _                                  => false
        };
    }

    public override CommandSingleCondition DeepCopy() =>
        new ActionCooldownCommandCondition
        {
            ComparisonType = ComparisonType,
            ActionID       = ActionID
        };

    protected override void DrawBody()
    {
        DrawLabel("比较类型", KnownColor.LightSkyBlue.ToVector4());
        ComparisonType = DrawEnumCombo("###ComparisonTypeCombo", ComparisonType);

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
        $"技能冷却 {ComparisonType.GetDescription()} {ActionID}";
}
