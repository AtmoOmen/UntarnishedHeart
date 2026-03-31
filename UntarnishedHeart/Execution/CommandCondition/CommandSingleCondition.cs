using Dalamud.Game.ClientState.Objects.Enums;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using OmenTools.OmenService;
using UntarnishedHeart.Execution.CommandCondition.Enums;

namespace UntarnishedHeart.Execution.CommandCondition;

public class CommandSingleCondition
{
    public CommandDetectType     DetectType     { get; set; }
    public CommandComparisonType ComparisonType { get; set; }
    public CommandTargetType     TargetType     { get; set; }
    public float                 Value          { get; set; }

    public void Draw(int i)
    {
        using var id    = ImRaii.PushId($"CommandSingleCondition-{i}");
        using var group = ImRaii.Group();

        using var table = ImRaii.Table("SingleConditionTable", 2);
        if (!table) return;

        ImGui.TableSetupColumn("名称", ImGuiTableColumnFlags.WidthFixed, ImGui.CalcTextSize("六个中国汉字").X);
        ImGui.TableSetupColumn("内容", ImGuiTableColumnFlags.WidthStretch);

        // 检测类型
        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        ImGui.AlignTextToFramePadding();
        ImGui.TextColored(KnownColor.LightSkyBlue.ToVector4(), "检测类型:");

        ImGui.TableNextColumn();
        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X - ImGui.GetStyle().ItemSpacing.X);

        using (var combo = ImRaii.Combo("###DetectTypeCombo", DetectType.GetDescription(), ImGuiComboFlags.HeightLargest))
        {
            if (combo)
            {
                foreach (var detectType in Enum.GetValues<CommandDetectType>())
                {
                    if (ImGui.Selectable($"{detectType.GetDescription()}", DetectType == detectType))
                        DetectType = detectType;
                    ImGuiOm.TooltipHover($"{detectType.GetDescription()}");
                }
            }
        }

        // 比较类型
        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        ImGui.AlignTextToFramePadding();
        ImGui.TextColored(KnownColor.LightSkyBlue.ToVector4(), "比较类型:");

        ImGui.TableNextColumn();
        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X - ImGui.GetStyle().ItemSpacing.X);

        using (var combo = ImRaii.Combo("###ComparisonTypeCombo", ComparisonType.GetDescription(), ImGuiComboFlags.HeightLargest))
        {
            if (combo)
            {
                foreach (var comparisonType in Enum.GetValues<CommandComparisonType>())
                {
                    if (ImGui.Selectable($"{comparisonType.GetDescription()}", ComparisonType == comparisonType))
                        ComparisonType = comparisonType;
                    ImGuiOm.TooltipHover($"{comparisonType.GetDescription()}");
                }
            }
        }

        // 比较类型
        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        ImGui.AlignTextToFramePadding();
        ImGui.TextColored(KnownColor.LightSkyBlue.ToVector4(), "目标类型:");

        ImGui.TableNextColumn();
        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X - ImGui.GetStyle().ItemSpacing.X);

        using (var combo = ImRaii.Combo("###TargetTypeCombo", TargetType.GetDescription(), ImGuiComboFlags.HeightLargest))
        {
            if (combo)
            {
                foreach (var targetType in Enum.GetValues<CommandTargetType>())
                {
                    if (ImGui.Selectable($"{targetType.GetDescription()}", TargetType == targetType))
                        TargetType = targetType;
                    ImGuiOm.TooltipHover($"{targetType.GetDescription()}");
                }
            }
        }

        // 比较类型
        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        ImGui.AlignTextToFramePadding();
        ImGui.TextColored(KnownColor.LightSkyBlue.ToVector4(), "值:");

        // 值
        var value = Value;
        ImGui.TableNextColumn();
        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X - ImGui.GetStyle().ItemSpacing.X);
        if (ImGui.InputFloat("###ValueInput", ref value))
            Value = value;
    }

    public unsafe bool IsConditionTrue()
    {
        if (TargetType == CommandTargetType.Target && TargetSystem.Instance()->Target == null) return true;

        switch (DetectType)
        {
            case CommandDetectType.Health:
                var health = TargetType switch
                {
                    CommandTargetType.Target => TargetManager.Target is IBattleChara target ? (float)target.CurrentHp / target.MaxHp * 100 : -1,
                    CommandTargetType.Self => DService.Instance().ObjectTable.LocalPlayer is IBattleChara target ? (float)target.CurrentHp / target.MaxHp * 100 : -1,
                    _ => -1
                };
                if (health == -1) return false;

                var healthValue = Value;
                return ComparisonType switch
                {
                    CommandComparisonType.GreaterThan => health > healthValue,
                    CommandComparisonType.LessThan    => health < healthValue,
                    CommandComparisonType.EqualTo     => health == healthValue,
                    CommandComparisonType.NotEqualTo  => health != healthValue,
                    _                                 => false
                };

            case CommandDetectType.Status:
                var statusID = (uint)Value;

                bool? hasStatus = TargetType switch
                {
                    CommandTargetType.Target => TargetManager.Target is IBattleChara { ObjectKind: ObjectKind.BattleNpc or ObjectKind.Player } target
                                                    ? target.ToBCStruct()->StatusManager.HasStatus(statusID)
                                                    : null,
                    CommandTargetType.Self => LocalPlayerState.HasStatus(statusID, out _),
                    _                      => null
                };
                if (hasStatus == null) return false;

                return ComparisonType switch
                {
                    CommandComparisonType.Has    => hasStatus.Value,
                    CommandComparisonType.NotHas => !hasStatus.Value,
                    _                            => false
                };

            case CommandDetectType.ActionCooldown:
                var actionID      = (uint)Value;
                var isOffCooldown = ActionManager.Instance()->IsActionOffCooldown(ActionType.Action, actionID);

                return ComparisonType switch
                {
                    CommandComparisonType.Finished    => isOffCooldown,
                    CommandComparisonType.NotFinished => !isOffCooldown,
                    _                                 => false
                };

            case CommandDetectType.ActionCastStart:
                if (TargetType != CommandTargetType.Target || ComparisonType != CommandComparisonType.Has) return false;
                if (TargetManager.Target is not IBattleChara targetCast) return false;
                if (!targetCast.IsCasting || targetCast.CastActionType != ActionType.Action) return false;

                var castActionID = (uint)Value;
                return targetCast.CastActionID == castActionID;

            default:
                return false;
        }
    }

    public override string ToString() => $"CommandSingleCondition_{DetectType}_{ComparisonType}_{TargetType}_{Value}";

    public static CommandSingleCondition Copy(CommandSingleCondition source) =>
        new()
        {
            DetectType     = source.DetectType,
            ComparisonType = source.ComparisonType,
            TargetType     = source.TargetType,
            Value          = source.Value
        };
}
