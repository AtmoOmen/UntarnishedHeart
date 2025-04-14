using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using ImGuiNET;

namespace UntarnishedHeart.Executor;

public class CommandCondition
{
    public List<CommandSingleCondition> Conditions   { get; set; } = [];
    public CommandRelationType          RelationType { get; set; } = CommandRelationType.And;
    public CommandExecuteType           ExecuteType  { get; set; } = CommandExecuteType.Wait;
    public float                        TimeValue    { get; set; }

    public void Draw()
    {
        ImGui.AlignTextToFramePadding();
        ImGui.TextColored(LightSkyBlue, "处理类型:");

        ImGui.SameLine();
        ImGui.SetNextItemWidth(150f * ImGuiHelpers.GlobalScale);
        using (var combo = ImRaii.Combo("###ExecuteTypeCombo", ExecuteType.ToString(), ImGuiComboFlags.HeightLargest))
        {
            if (combo)
            {
                foreach (var executeType in Enum.GetValues<CommandExecuteType>())
                {
                    if (ImGui.Selectable($"{executeType.ToString()}", ExecuteType == executeType))
                        ExecuteType = executeType;
                    ImGuiOm.TooltipHover($"{executeType.GetDescription()}");
                }
            }
        }

        ImGui.AlignTextToFramePadding();
        ImGui.TextColored(LightSkyBlue, "时间数值:");

        var timeValue = TimeValue;
        ImGui.SameLine();
        ImGui.SetNextItemWidth(150f * ImGuiHelpers.GlobalScale);
        if (ImGui.InputFloat("(ms)###TimeValueInput", ref timeValue, 0, 0))
            TimeValue = timeValue;
        ImGuiOm.TooltipHover("若处理类型为:\n"   +
                             "Wait\t无效果\n" +
                             "Pass\t无效果\n" +
                             "Repeat\t每执行一轮间的时间间隔");

        ImGui.AlignTextToFramePadding();
        ImGui.TextColored(LightSkyBlue, "关系类型:");

        ImGui.SameLine();
        ImGui.SetNextItemWidth(100f * ImGuiHelpers.GlobalScale);
        using (var combo = ImRaii.Combo("###RelationTypeCombo", RelationType.ToString(), ImGuiComboFlags.HeightLargest))
        {
            if (combo)
            {
                foreach (var relationType in Enum.GetValues<CommandRelationType>())
                {
                    if (ImGui.Selectable($"{relationType.ToString()}", RelationType == relationType))
                        RelationType = relationType;
                    ImGuiOm.TooltipHover($"{relationType.GetDescription()}");
                }
            }
        }

        ImGui.SameLine();
        if (ImGuiOm.ButtonIconWithText(FontAwesomeIcon.Plus, "添加新条件", true))
            Conditions.Add(new());

        ImGui.Spacing();

        for (var i = 0; i < Conditions.Count; i++)
        {
            var step = Conditions[i];

            using var node = ImRaii.TreeNode($"第 {i + 1} 条###Step-{i}");
            if (!node) continue;

            var ret = step.Draw(i, Conditions.Count);
            Action executorOperationAction = ret switch
            {
                StepOperationType.Delete   => () => Conditions.RemoveAt(i),
                StepOperationType.MoveDown => () => Conditions.Swap(i, i + 1),
                StepOperationType.MoveUp   => () => Conditions.Swap(i, i - 1),
                StepOperationType.Copy     => () => Conditions.Insert(i  + 1, step.Copy()),
                StepOperationType.Pass     => () => { },
                _                          => () => { }
            };
            executorOperationAction();
        }
    }

    public bool IsConditionsTrue() =>
        RelationType switch
        {
            CommandRelationType.And => Conditions.All(x => x.IsConditionTrue()),
            CommandRelationType.Or  => Conditions.Any(x => x.IsConditionTrue()),
            _                       => false
        };
}

public class CommandSingleCondition
{
    public CommandDetectType     DetectType     { get; set; }
    public CommandComparisonType ComparisonType { get; set; }
    public CommandTargetType     TargetType     { get; set; }
    public float                 Value          { get; set; }

    public StepOperationType Draw(int i, int count)
    {
        using var id = ImRaii.PushId($"CommandSingleCondition-{i}");
        
        #region 步骤信息

        ImGui.AlignTextToFramePadding();
        ImGui.Text("操作:");

        using (ImRaii.Group())
        {
            ImGui.SameLine();
            if (ImGuiOm.ButtonIconWithText(FontAwesomeIcon.TrashAlt, "删除", true))
                return StepOperationType.Delete;

            if (i > 0)
            {
                ImGui.SameLine();
                if (ImGuiOm.ButtonIconWithText(FontAwesomeIcon.ArrowUp, "上移", true))
                    return StepOperationType.MoveUp;
            }

            if (i < count - 1)
            {
                ImGui.SameLine();
                if (ImGuiOm.ButtonIconWithText(FontAwesomeIcon.ArrowDown, "下移", true))
                    return StepOperationType.MoveDown;
            }

            ImGui.SameLine();
            if (ImGuiOm.ButtonIconWithText(FontAwesomeIcon.Copy, "复制", true))
                return StepOperationType.Copy;
        }

        #endregion
        
        using var table = ImRaii.Table("SingleConditionTable", 2);
        if (!table) return StepOperationType.Pass;
        
        ImGui.TableSetupColumn("名称", ImGuiTableColumnFlags.WidthFixed, ImGui.CalcTextSize("六个中国汉字").X);
        ImGui.TableSetupColumn("内容", ImGuiTableColumnFlags.WidthStretch);
        
        // 检测类型
        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        ImGui.AlignTextToFramePadding();
        ImGui.TextColored(LightSkyBlue, "检测类型:");
        
        ImGui.TableNextColumn();
        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X - ImGui.GetStyle().ItemSpacing.X);
        using (var combo = ImRaii.Combo("###DetectTypeCombo", DetectType.ToString(), ImGuiComboFlags.HeightLargest))
        {
            if (combo)
            {
                foreach (var detectType in Enum.GetValues<CommandDetectType>())
                {
                    if (ImGui.Selectable($"{detectType.ToString()}", DetectType == detectType))
                        DetectType = detectType;
                    ImGuiOm.TooltipHover($"{detectType.GetDescription()}");
                }
            }
        }
        
        // 比较类型
        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        ImGui.AlignTextToFramePadding();
        ImGui.TextColored(LightSkyBlue, "比较类型:");
        
        ImGui.TableNextColumn();
        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X - ImGui.GetStyle().ItemSpacing.X);
        using (var combo = ImRaii.Combo("###ComparisonTypeCombo", ComparisonType.ToString(), ImGuiComboFlags.HeightLargest))
        {
            if (combo)
            {
                foreach (var comparisonType in Enum.GetValues<CommandComparisonType>())
                {
                    if (ImGui.Selectable($"{comparisonType.ToString()}", ComparisonType == comparisonType))
                        ComparisonType = comparisonType;
                    ImGuiOm.TooltipHover($"{comparisonType.GetDescription()}");
                }
            }
        }
        
        // 比较类型
        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        ImGui.AlignTextToFramePadding();
        ImGui.TextColored(LightSkyBlue, "目标类型:");
        
        ImGui.TableNextColumn();
        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X - ImGui.GetStyle().ItemSpacing.X);
        using (var combo = ImRaii.Combo("###TargetTypeCombo", TargetType.ToString(), ImGuiComboFlags.HeightLargest))
        {
            if (combo)
            {
                foreach (var targetType in Enum.GetValues<CommandTargetType>())
                {
                    if (ImGui.Selectable($"{targetType.ToString()}", TargetType == targetType))
                        TargetType = targetType;
                    ImGuiOm.TooltipHover($"{targetType.GetDescription()}");
                }
            }
        }
        
        // 比较类型
        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        ImGui.AlignTextToFramePadding();
        ImGui.TextColored(LightSkyBlue, "值:");
        
        // 值
        var value = Value;
        ImGui.TableNextColumn();
        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X - ImGui.GetStyle().ItemSpacing.X);
        if (ImGui.InputFloat("###ValueInput", ref value, 0, 0))
            Value = value;
        
        return StepOperationType.Pass;
    }

    public unsafe bool IsConditionTrue()
    {
        switch (DetectType)
        {
            case CommandDetectType.Health:
                var health = TargetType switch
                {
                    CommandTargetType.Target => DService.Targets.Target is IBattleChara target ? 
                                                    (int)((double)target.CurrentHp / target.MaxHp * 100) : 
                                                    -1,
                    CommandTargetType.Self   => DService.ClientState.LocalPlayer is IBattleChara target ? 
                                                    (int)((double)target.CurrentHp / target.MaxHp * 100) : 
                                                    -1,
                    _                        => -1
                };
                if (health == -1) return false;
                
                var healthValue = (int)Value;
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
                var targetObj = TargetSystem.Instance()->Target;

                bool? hasStatus = TargetType switch
                {
                    CommandTargetType.Target => targetObj != null && targetObj->ObjectKind is ObjectKind.Pc or ObjectKind.BattleNpc ? 
                                                    ((BattleChara*)targetObj)->StatusManager.HasStatus(statusID) : 
                                                    null,
                    CommandTargetType.Self => Control.GetLocalPlayer()->StatusManager.HasStatus(statusID),
                    _                      => null
                };
                Debug($"测试判断状态: {statusID} {hasStatus}");
                if (hasStatus == null) return false;

                return ComparisonType switch
                {
                    CommandComparisonType.Has    => hasStatus.Value,
                    CommandComparisonType.NotHas => !hasStatus.Value,
                    _                            => false
                };
            default:
                return false;
        }
    }

    public override string ToString() => $"CommandSingleCondition_{DetectType}_{ComparisonType}_{TargetType}_{Value}";

    public CommandSingleCondition Copy() =>
        new()
        {
            DetectType     = DetectType,
            ComparisonType = ComparisonType,
            TargetType     = TargetType,
            Value          = Value,
        };
}

public enum CommandDetectType
{
    [Description("生命值百分比, 有效比较值: GreaterThan / LessThan / EqualTo / NotEqualTo")]
    Health,
    [Description("状态效果, 有效比较值: Has / NotHas")]
    Status,
}

public enum CommandComparisonType
{
     [Description("大于")]
     GreaterThan,
     [Description("小于")]
     LessThan,
     [Description("等于")]
     EqualTo,
     [Description("不等于")]
     NotEqualTo,
     [Description("不拥有")]
     Has,
     [Description("拥有")]
     NotHas,
}

public enum CommandTargetType
{
    [Description("自身")]
    Self,
    [Description("目标")]
    Target,
}

public enum CommandRelationType
{
    [Description("和关系, 即全部条件满足才算满足")]
    And,
    [Description("或关系, 即任一条件满足就算满足")]
    Or
}

public enum CommandExecuteType
{
    [Description("若不满足条件, 则等待满足条件后再执行文本指令")]
    Wait,
    [Description("若不满足条件, 则跳过执行文本指令")]
    Pass,
    [Description("若不满足条件, 则重复执行文本指令, 若满足, 则仅执行一次")]
    Repeat,
}
