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
    
    private CommandSingleCondition? ConditionToCopy;

    public void Draw()
    {
        ImGui.AlignTextToFramePadding();
        ImGui.TextColored(LightSkyBlue, "处理类型:");

        ImGui.SameLine();
        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X - ImGui.GetStyle().ItemSpacing.X);
        using (var combo = ImRaii.Combo("###ExecuteTypeCombo", ExecuteType.GetDescription(), ImGuiComboFlags.HeightLargest))
        {
            if (combo)
            {
                foreach (var executeType in Enum.GetValues<CommandExecuteType>())
                {
                    if (ImGui.Selectable($"{executeType.GetDescription()}", ExecuteType == executeType))
                        ExecuteType = executeType;
                    ImGuiOm.TooltipHover($"{executeType.GetDescription()}");
                }
            }
        }

        ImGui.AlignTextToFramePadding();
        ImGui.TextColored(LightSkyBlue, "时间数值:");

        var timeValue = TimeValue;
        ImGui.SameLine();
        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X - ImGui.CalcTextSize(" (ms) ").X);
        if (ImGui.InputFloat("(ms)###TimeValueInput", ref timeValue, 0, 0))
            TimeValue = timeValue;
        ImGuiOm.TooltipHover("若处理类型为:\n"   +
                             "Wait\t无效果\n" +
                             "Pass\t无效果\n" +
                             "Repeat\t每执行一轮间的时间间隔");

        ImGui.AlignTextToFramePadding();
        ImGui.TextColored(LightSkyBlue, "关系类型:");

        ImGui.SameLine();
        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X - ImGui.GetStyle().ItemSpacing.X);
        using (var combo = ImRaii.Combo("###RelationTypeCombo", RelationType.GetDescription(), ImGuiComboFlags.HeightLargest))
        {
            if (combo)
            {
                foreach (var relationType in Enum.GetValues<CommandRelationType>())
                {
                    if (ImGui.Selectable($"{relationType.GetDescription()}", RelationType == relationType))
                        RelationType = relationType;
                    ImGuiOm.TooltipHover($"{relationType.GetDescription()}");
                }
            }
        }

        if (ImGuiOm.ButtonIconWithText(FontAwesomeIcon.Plus, "添加新条件", true))
            Conditions.Add(new());

        ImGui.Spacing();

        for (var i = 0; i < Conditions.Count; i++)
        {
            var step = Conditions[i];

            using var node = ImRaii.TreeNode($"第 {i + 1} 条###Step-{i}");
            if (!node)
            {
                DrawStepContextMenu(i, step);
                continue;
            }

            step.Draw(i);
            DrawStepContextMenu(i, step);
        }
        
        return;

        void DrawStepContextMenu(int i, CommandSingleCondition step)
        {
            var contextOperation = StepOperationType.Pass;

            if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
                ImGui.OpenPopup($"ConditionContentMenu_{i}");

            using var context = ImRaii.ContextPopupItem($"ConditionContentMenu_{i}");
            if (!context) return;
            
            if (ImGui.MenuItem("复制"))
                ConditionToCopy = step.Copy();

            if (ConditionToCopy != null)
            {
                using (ImRaii.Group())
                {
                    if (ImGui.MenuItem("粘贴至本步"))
                        contextOperation = StepOperationType.Paste;

                    if (ImGui.MenuItem("向上插入粘贴"))
                        contextOperation = StepOperationType.PasteUp;

                    if (ImGui.MenuItem("向下插入粘贴"))
                        contextOperation = StepOperationType.PasteDown;
                }
            }

            if (ImGui.MenuItem("删除"))
                contextOperation = StepOperationType.Delete;

            if (i > 0)
                if (ImGui.MenuItem("上移"))
                    contextOperation = StepOperationType.MoveUp;

            if (i < Conditions.Count - 1)
                if (ImGui.MenuItem("下移"))
                    contextOperation = StepOperationType.MoveDown;

            ImGui.Separator();

            if (ImGui.MenuItem("向上插入新步骤"))
                contextOperation = StepOperationType.InsertUp;

            if (ImGui.MenuItem("向下插入新步骤"))
                contextOperation = StepOperationType.InsertDown;

            ImGui.Separator();

            if (ImGui.MenuItem("复制并插入本步骤"))
                contextOperation = StepOperationType.PasteCurrent;

            Action contextOperationAction = contextOperation switch
            {
                StepOperationType.Delete => () => Conditions.RemoveAt(i),
                StepOperationType.MoveDown => () =>
                {
                    var index = i + 1;
                    Conditions.Swap(i, index);
                },
                StepOperationType.MoveUp => () =>
                {
                    var index = i - 1;
                    Conditions.Swap(i, index);
                },
                StepOperationType.Pass => () => { },
                StepOperationType.Paste => () =>
                {
                    Conditions[i]    = ConditionToCopy;
                },
                StepOperationType.PasteUp => () =>
                {
                    Conditions.Insert(i, ConditionToCopy.Copy());
                },
                StepOperationType.PasteDown => () =>
                {
                    var index = i + 1;
                    Conditions.Insert(index, ConditionToCopy.Copy());
                },
                StepOperationType.InsertUp => () =>
                {
                    Conditions.Insert(i, new());
                },
                StepOperationType.InsertDown => () =>
                {
                    var index = i + 1;
                    Conditions.Insert(index, new());
                },
                StepOperationType.PasteCurrent => () =>
                {
                    Conditions.Insert(i, step.Copy());
                },
                _ => () => { }
            };
            contextOperationAction();
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
        ImGui.TextColored(LightSkyBlue, "检测类型:");
        
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
        ImGui.TextColored(LightSkyBlue, "比较类型:");
        
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
        ImGui.TextColored(LightSkyBlue, "目标类型:");
        
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
        ImGui.TextColored(LightSkyBlue, "值:");
        
        // 值
        var value = Value;
        ImGui.TableNextColumn();
        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X - ImGui.GetStyle().ItemSpacing.X);
        if (ImGui.InputFloat("###ValueInput", ref value, 0, 0))
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
    [Description("生命值百分比")]
    Health,
    [Description("状态效果")]
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
     [Description("拥有")]
     Has,
     [Description("不拥有")]
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
