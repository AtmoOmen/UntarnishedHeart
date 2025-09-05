using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using ImGuiNET;
using OmenTools.Service;

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
        ImGui.SetNextItemWidth(300f * ImGuiHelpers.GlobalScale);
        using (var combo = ImRaii.Combo("处理类型###ExecuteTypeCombo", ExecuteType.GetDescription(), ImGuiComboFlags.HeightLargest))
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

        if (ExecuteType == CommandExecuteType.Repeat)
        {
            var timeValue = TimeValue;
            ImGui.SetNextItemWidth(300f * ImGuiHelpers.GlobalScale);
            if (ImGui.InputFloat("重复间隔 (ms)###TimeValueInput", ref timeValue, 0, 0))
                TimeValue = timeValue;
            ImGuiOm.TooltipHover("每执行一轮间的时间间隔");
        }
        
        ImGui.SetNextItemWidth(300f * ImGuiHelpers.GlobalScale);
        using (var combo = ImRaii.Combo("关系类型###RelationTypeCombo", RelationType.GetDescription(), ImGuiComboFlags.HeightLargest))
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
                ConditionToCopy = CommandSingleCondition.Copy(step);

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
                    Conditions[i] = CommandSingleCondition.Copy(ConditionToCopy);
                },
                StepOperationType.PasteUp => () =>
                {
                    Conditions.Insert(i, CommandSingleCondition.Copy(ConditionToCopy));
                },
                StepOperationType.PasteDown => () =>
                {
                    var index = i + 1;
                    Conditions.Insert(index, CommandSingleCondition.Copy(ConditionToCopy));
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
                    Conditions.Insert(i, CommandSingleCondition.Copy(step));
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

    public static CommandCondition Copy(CommandCondition source)
    {
        var conditions = new List<CommandSingleCondition>();
        source.Conditions.ForEach(x => conditions.Add(CommandSingleCondition.Copy(x)));
        
        return new CommandCondition
        {
            Conditions   = conditions.ToList(),
            RelationType = source.RelationType,
            ExecuteType  = source.ExecuteType,
            TimeValue    = source.TimeValue,
        };
    }
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
                                                    (float)target.CurrentHp / target.MaxHp * 100 : 
                                                    -1,
                    CommandTargetType.Self   => DService.ObjectTable.LocalPlayer is IBattleChara target ? 
                                                    (float)target.CurrentHp / target.MaxHp * 100 : 
                                                    -1, 
                    _                        => -1
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
                var statusID  = (uint)Value;
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
                if (DService.Targets.Target is not IBattleChara targetCast) return false;
                if (!targetCast.IsCasting || targetCast.CastActionType != ActionType.Action) return false;

                var castActionID = (uint)Value;
                return targetCast.CastActionId == castActionID;
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
            Value          = source.Value,
        };
}

public enum CommandDetectType
{
    [Description("生命值百分比 (大于/小于/等于/不等于)")]
    Health,
    
    [Description("状态效果 (拥有/不拥有)")]
    Status,
    
    [Description("技能冷却 [自身] (完成/未完成)")]
    ActionCooldown,
    
    [Description("技能咏唱开始 [自身] (拥有)")]
    ActionCastStart,
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
     [Description("完成")]
     Finished,
     [Description("未完成")]
     NotFinished,
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
    [Description("和关系 (全部条件满足才算满足)")]
    And,
    [Description("或关系 (任一条件满足就算满足)")]
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
