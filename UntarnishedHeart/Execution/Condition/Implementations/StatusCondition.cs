using Dalamud.Game.ClientState.Objects.Enums;
using Lumina.Excel.Sheets;
using OmenTools.Interop.Game.Lumina;
using OmenTools.OmenService;
using UntarnishedHeart.Execution.Condition.Enums;

namespace UntarnishedHeart.Execution.Condition;

public sealed class StatusCondition : Condition
{
    public override ConditionDetectType Kind => ConditionDetectType.Status;

    public PresenceComparisonType ComparisonType { get; set; } = PresenceComparisonType.Has;

    public ConditionTargetType TargetType { get; set; } = ConditionTargetType.Target;

    public uint StatusID { get; set; }

    public override unsafe bool Evaluate(in ConditionContext context)
    {
        var hasStatus = TargetType switch
        {
            ConditionTargetType.Self => LocalPlayerState.HasStatus(StatusID, out _),
            ConditionTargetType.Target => context.Target is { ObjectKind: ObjectKind.BattleNpc or ObjectKind.Player } target && target.ToBCStruct()->StatusManager.HasStatus(StatusID),
            _ => false
        };

        return ComparisonType switch
        {
            PresenceComparisonType.Has    => hasStatus,
            PresenceComparisonType.NotHas => !hasStatus,
            _                             => false
        };
    }

    public override Condition DeepCopy() =>
        new StatusCondition
        {
            ComparisonType = ComparisonType,
            TargetType     = TargetType,
            StatusID       = StatusID
        };

    protected override void DrawBody()
    {
        DrawLabel("比较类型", KnownColor.LightSkyBlue.ToVector4());
        ComparisonType = DrawEnumCombo("###ComparisonTypeCombo", ComparisonType);

        DrawLabel("目标类型", KnownColor.LightSkyBlue.ToVector4());
        TargetType = DrawEnumCombo("###TargetTypeCombo", TargetType);

        DrawLabel("状态 ID", KnownColor.LightSkyBlue.ToVector4());
        var statusID = (int)StatusID;
        if (ImGui.InputInt("###StatusIdInput", ref statusID))
            StatusID = (uint)Math.Max(0, statusID);
        
        if (LuminaGetter.TryGetRow((uint)statusID, out Status row))
        {
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.TableNextColumn();
            
            ImGui.TextUnformatted($"({row.Name})");
        }
    }

    protected override string Describe() =>
        $"状态效果 {TargetType.GetDescription()} {ComparisonType.GetDescription()} {StatusID}";
}
