using Lumina.Excel.Sheets;
using OmenTools.Interop.Game.Lumina;
using UntarnishedHeart.Execution.Condition.Enums;

namespace UntarnishedHeart.Execution.Condition;

public sealed class StatusCondition : ConditionBase
{
    public override ConditionDetectType Kind => ConditionDetectType.Status;

    public PresenceComparisonType ComparisonType { get; set; } = PresenceComparisonType.Has;

    public ConditionTargetType TargetType { get; set; } = ConditionTargetType.Target;

    public uint StatusID { get; set; }

    public override bool Evaluate(in ConditionContext context)
    {
        var target    = ResolveTarget(context, TargetType);
        var hasStatus = target?.StatusList.HasStatus(StatusID) == true;
        return ComparisonType == PresenceComparisonType.Has ? hasStatus : !hasStatus;
    }

    protected override bool EqualsCore(ConditionBase other) =>
        other is StatusCondition condition         &&
        ComparisonType == condition.ComparisonType &&
        TargetType     == condition.TargetType     &&
        StatusID       == condition.StatusID;

    protected override int GetCoreHashCode() => HashCode.Combine((int)ComparisonType, (int)TargetType, StatusID);

    public override ConditionBase DeepCopy() =>
        CopyBasePropertiesTo
        (
            new StatusCondition
            {
                ComparisonType = ComparisonType,
                TargetType     = TargetType,
                StatusID       = StatusID
            }
        );

    protected override void DrawBody()
    {
        DrawLabel("比较类型", KnownColor.LightSkyBlue.ToVector4());
        ComparisonType = DrawEnumCombo("###ComparisonTypeCombo", ComparisonType);

        TargetType = DrawTargetType("###TargetTypeCombo", TargetType);

        DrawLabel("状态 ID", KnownColor.LightSkyBlue.ToVector4());
        var statusID = StatusID;
        if (ImGui.InputUInt("###StatusIdInput", ref statusID))
            StatusID = statusID;

        if (LuminaGetter.TryGetRow(StatusID, out Status row))
        {
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.TableNextColumn();
            ImGui.TextUnformatted($"({row.Name})");
        }
    }
}
