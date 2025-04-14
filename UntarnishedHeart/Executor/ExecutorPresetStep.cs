using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Utility;
using Dalamud.Interface;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using ImGuiNET;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Numerics;
using Dalamud.Game.ClientState.Conditions;
using UntarnishedHeart.Utils;
using Dalamud.Game.ClientState.Objects.Enums;
using UntarnishedHeart.Managers;

namespace UntarnishedHeart.Executor;

public class ExecutorPresetStep : IEquatable<ExecutorPresetStep>
{
    public string     Note               { get; set; } = string.Empty;
    public bool       StopWhenBusy       { get; set; }
    public bool       StopInCombat       { get; set; } = true;
    public Vector3    Position           { get; set; }
    public MoveType   MoveType           { get; set; } = MoveType.无;
    public bool       WaitForGetClose    { get; set; }
    public uint       DataID             { get; set; }
    public ObjectKind ObjectKind         { get; set; } = ObjectKind.BattleNpc;
    public bool       WaitForTarget      { get; set; } = true;
    public bool       InteractWithTarget { get; set; }
    public string     Commands           { get; set; } = string.Empty;
    public int        Delay              { get; set; } = 5000;

    public ExecutorStepOperationType Draw(int i, int count)
    {
        using var id = ImRaii.PushId($"Step-{i}");
        using var group = ImRaii.Group();

        ImGui.AlignTextToFramePadding();
        ImGui.Text("操作:");

        using (ImRaii.Group())
        {
            ImGui.SameLine();
            if (ImGuiOm.ButtonIconWithText(FontAwesomeIcon.TrashAlt, "删除", true))
                return ExecutorStepOperationType.DELETE;

            if (i > 0)
            {
                ImGui.SameLine();
                if (ImGuiOm.ButtonIconWithText(FontAwesomeIcon.ArrowUp, "上移", true))
                    return ExecutorStepOperationType.MOVEUP;
            }

            if (i < count - 1)
            {
                ImGui.SameLine();
                if (ImGuiOm.ButtonIconWithText(FontAwesomeIcon.ArrowDown, "下移", true))
                    return ExecutorStepOperationType.MOVEDOWN;
            }
        
            ImGui.SameLine();
            if (ImGuiOm.ButtonIconWithText(FontAwesomeIcon.Copy, "复制", true))
                return ExecutorStepOperationType.COPY;
        }

        var stepName = Note;
        ImGuiOm.CompLabelLeft(
            "备注:", 200f * ImGuiHelpers.GlobalScale,
            () => ImGui.InputText("###StepNoteInput", ref stepName, 128));
        if (ImGui.IsItemDeactivatedAfterEdit())
            Note = stepName;
        
        ImGui.Spacing();
        
        var stepStopInCombat = StopInCombat;
        if (ImGuiOm.CompLabelLeft(
                "若已进入战斗, 则等待:", 200f * ImGuiHelpers.GlobalScale,
                () => ImGui.Checkbox("###StepStopInCombatInput", ref stepStopInCombat)))
            StopInCombat = stepStopInCombat;
        ImGuiOm.TooltipHover("若勾选, 则在执行此步时检查是否进入战斗状态, 若已进入, 则阻塞步骤执行");
        
        var stepStopWhenBusy = StopWhenBusy;
        if (ImGuiOm.CompLabelLeft(
                "若为忙碌状态, 则等待:", 200f * ImGuiHelpers.GlobalScale,
                () => ImGui.Checkbox("###StepStopWhenBusyInput", ref stepStopWhenBusy)))
            StopWhenBusy = stepStopWhenBusy;
        ImGuiOm.TooltipHover("若勾选, 则在执行此步时检查是否正处于忙碌状态 (如: 过图加载, 交互等), 若已进入, 则阻塞步骤执行");

        ImGui.Spacing();
        
        using (var treeNode = ImRaii.TreeNode("坐标"))
        {
            if (treeNode)
            {
                ImGui.AlignTextToFramePadding();
                ImGui.TextColored(LightSkyBlue, "方式:");
                
                ImGui.SameLine();
                ImGui.SetNextItemWidth(200f * ImGuiHelpers.GlobalScale);
                using (var combo = ImRaii.Combo("###StepMoveTypeCombo", MoveType.ToString()))
                {
                    if (combo)
                    {
                        foreach (var moveType in Enum.GetValues<MoveType>())
                        {
                            if (ImGui.Selectable(moveType.ToString(), MoveType == moveType))
                                MoveType = moveType;
                        }
                    }
                }
                ImGuiOm.TooltipHover("若设置为 无, 则代表使用插件配置的默认移动方式");

                using (ImRaii.Group())
                {
                    var stepPosition = Position;
                    ImGui.AlignTextToFramePadding();
                    ImGui.TextColored(LightSkyBlue, "位置:");
                
                    ImGui.SameLine();
                    ImGui.SetNextItemWidth(200f * ImGuiHelpers.GlobalScale);
                    if (ImGui.InputFloat3("###StepPositionInput", ref stepPosition))
                        Position = stepPosition;
                    ImGuiOm.TooltipHover("若不想执行移动, 请将坐标设置为 <0, 0, 0>");

                    ImGui.SameLine();
                    if (ImGuiOm.ButtonIcon("GetPosition", FontAwesomeIcon.Bullseye, "取当前位置", true))
                    {
                        if (DService.ClientState.LocalPlayer is { } localPlayer)
                            Position = localPlayer.Position;
                    }

                    ImGui.SameLine();
                    if (ImGuiOm.ButtonIcon("TPToThis", FontAwesomeIcon.Flag, "传送到此位置", true))
                        GameFunctions.Teleport(stepPosition);
                }

                using (ImRaii.Group())
                {
                    ImGui.AlignTextToFramePadding();
                    ImGui.TextColored(LightSkyBlue, "等待接近坐标后再继续:");
                
                    var waitForGetClose = WaitForGetClose;
                    ImGui.SameLine();
                    if (ImGui.Checkbox("###WaitForGetCloseInput", ref waitForGetClose))
                        WaitForGetClose = waitForGetClose;
                }
                ImGuiOm.TooltipHover("若勾选, 则会等待完全接近坐标后再继续执行下面的操作");
            }
            else
                ImGuiOm.TooltipHover("在执行完上方的战斗状态检查后, 会按照此处的配置移动到指定的坐标");
        }
        
        ImGui.Spacing();
        
        using (var treeNode = ImRaii.TreeNode("目标"))
        {
            if (treeNode)
            {
                using (var table = ImRaii.Table("TargetInfoTable", 2))
                {
                    if (table)
                    {
                        ImGui.TableSetupColumn("标签", ImGuiTableColumnFlags.WidthFixed,
                                               ImGui.CalcTextSize("Data ID:").X);
                        ImGui.TableSetupColumn("操作", ImGuiTableColumnFlags.WidthStretch, 50);

                        ImGui.TableNextRow();

                        ImGui.TableNextColumn();
                        ImGui.AlignTextToFramePadding();
                        ImGui.TextColored(LightSkyBlue, "类型:");

                        ImGui.TableNextColumn();
                        ImGui.SetNextItemWidth(200f * ImGuiHelpers.GlobalScale);
                        using (var combo = ImRaii.Combo("###TargetObjectKindCombo", ObjectKind.ToString()))
                        {
                            if (combo)
                            {
                                foreach (var objectKind in Enum.GetValues<ObjectKind>())
                                {
                                    if (ImGui.Selectable(objectKind.ToString(), ObjectKind == objectKind))
                                        ObjectKind = objectKind;
                                }
                            }
                        }

                        ImGui.TableNextRow();

                        ImGui.TableNextColumn();
                        ImGui.AlignTextToFramePadding();
                        ImGui.TextColored(LightSkyBlue, "Data ID:");
                        ImGuiOm.TooltipHover("设置为 0 则为不自动执行任何选中操作");

                        ImGui.TableNextColumn();
                        var stepDataID = DataID;
                        ImGui.SetNextItemWidth(200f * ImGuiHelpers.GlobalScale);
                        if (ImGuiOm.InputUInt("###StepDatIDInput", ref stepDataID))
                            DataID = stepDataID;

                        ImGui.SameLine();
                        if (ImGuiOm.ButtonIcon("GetTarget", FontAwesomeIcon.Crosshairs, "取当前目标", true))
                        {
                            if (DService.Targets.Target is { } target)
                            {
                                DataID     = target.DataId;
                                ObjectKind = target.ObjectKind;
                            }
                        }
                    }
                }
                
                ImGui.Spacing();

                using (ImRaii.PushColor(ImGuiCol.Text, LightSkyBlue))
                {
                    var waitForTarget = WaitForTarget;
                    if (ImGui.Checkbox("等待目标被选中", ref waitForTarget))
                        WaitForTarget = waitForTarget;
                }
                ImGuiOm.TooltipHover("勾选后, 则会阻塞进程持续查找对应目标并尝试选中, 直至任一目标被选中");
                
                using (ImRaii.PushColor(ImGuiCol.Text, LightSkyBlue))
                {
                    var interactWithTarget = InteractWithTarget;
                    if (ImGui.Checkbox("交互此目标", ref interactWithTarget))
                        InteractWithTarget = interactWithTarget;
                }
                ImGuiOm.TooltipHover("勾选后, 则会尝试与当前目标进行交互, 若未选中任一目标则跳过\n\n" +
                                     "注: 请自行确保位于一个可交互到目标的坐标");
            }
            else
                ImGuiOm.TooltipHover("在开始向上方配置的坐标移动后, 会按照此处的配置选中指定的目标");
        }
        
        ImGui.Spacing();
        
        using (ImRaii.Group())
        {
            var stepCommands = Commands;
            ImGui.Text("文本指令:");
            if (ImGui.InputTextMultiline("###CommandsInput", ref stepCommands, 1024,
                                         new(ImGui.GetContentRegionAvail().X - ImGui.GetStyle().ItemSpacing.X,
                                             ImGui.GetTextLineHeightWithSpacing() * 2.5f)))
                Commands = stepCommands;
        }
        ImGuiOm.TooltipHover(string.IsNullOrWhiteSpace(Commands)
                                 ? "在执行完上方的目标选择后, 会按照此处的配置选中自定义文本指令\n" +
                                   "支持多条文本指令, 一行一条"
                                 : Commands);
        
        ImGui.Text("等待接近目标位置");
        ImGuiOm.HelpMarker("只要上方 \"坐标\" 内的坐标设置不为 <0, 0, 0>, 则始终会执行接近检测并堵塞, 直至目标 2D 距离 ≤ 2",
                           20f * ImGuiHelpers.GlobalScale, useStaticFont: true);
        
        ImGui.Spacing();

        using (ImRaii.Group())
        {
            var stepDelay = Delay;
            if (ImGuiOm.CompLabelLeft(
                    "等待:", 200f * ImGuiHelpers.GlobalScale,
                    () =>
                    {
                        var input            = ImGui.InputInt("###StepDelayInput", ref stepDelay, 0, 0);
                        if (input) stepDelay = Math.Max(0, stepDelay);
                        return input;
                    }))
                Delay = stepDelay;

            ImGui.SameLine();
            ImGui.Text("(ms)");
        }
        ImGuiOm.TooltipHover("在开始下一步骤前, 需要等待的时间");


        return ExecutorStepOperationType.PASS;
    }

    public List<Action> GetTasks(TaskHelper t)
        =>
        [
            // 检查进战状态
            () => t.Enqueue(() => !StopInCombat || !DService.Condition[ConditionFlag.InCombat], $"检查进战状态: {Note}"),
            // 检查忙碌状态
            () => t.Enqueue(() => !StopWhenBusy || (!OccupiedInEvent && IsScreenReady()), $"检查忙碌状态: {Note}"),
            // 执行移动
            () => t.Enqueue(() =>
            {
                if (Position == default(Vector3)) return true;
                
                var finalMoveType = MoveType == MoveType.无 ? Service.Config.MoveType : MoveType;
                if (finalMoveType == MoveType.无) return true;
                
                switch (finalMoveType)
                {
                    case MoveType.寻路:
                        GameFunctions.PathFindStart(Position);
                        break;
                    case MoveType.传送:
                        GameFunctions.Teleport(Position);
                        break;
                }
                return true;
            }, $"移动至目标位置: {Note}"),
            // 执行完全接近目标等待
            () => t.Enqueue(() =>
            {
                if (Position == default(Vector3) || !WaitForGetClose) return true;
                
                if (DService.ClientState.LocalPlayer is not { } localPlayer) return false;
                if (!Throttler.Throttle("接近目标位置节流")) return false;

                return Vector2.DistanceSquared(localPlayer.Position.ToVector2(), Position.ToVector2()) <= 4;
            }, $"等待完全接近目标位置: {Note}"),
            // 执行目标选中
            () => t.Enqueue(() =>
            {
                // 目标配置为 0, 不选中
                if (DataID == 0) return true;
                if (!Throttler.Throttle("选中目标节流")) return false;

                TargetObject();
                return !WaitForTarget || DService.Targets.Target != null;
            }, $"选中目标: {Note}"),
            // 执行目标交互
            () => t.Enqueue(() =>
            {
                if (DataID == 0 || !InteractWithTarget || DService.Targets.Target is not { } target) return true;
                return target.TargetInteract();
            }, $"交互预设目标: {Note}"),
            // 等待目标交互完成
            () => t.DelayNext(InteractWithTarget ? 200 : 0, $"延迟 200 毫秒, 等待交互开始: {Note}"),
            () => t.Enqueue(() => !InteractWithTarget || (!OccupiedInEvent && IsScreenReady()), $"等待目标交互完成: {Note}"),
            // 执行文本指令
            () =>
            {
                foreach (var command in Commands.Split('\n'))
                {
                    t.Enqueue(() => ChatHelper.Instance.SendMessage(command), $"使用文本指令: {Note} {command}");
                    t.DelayNext(100, $"使用文本指令节流: {Note} {command}");
                }
            },
            // 等待接近目标位置
            () => t.Enqueue(() =>
            {
                if (Position == default(Vector3)) return true;
                if (DService.ClientState.LocalPlayer is not { } localPlayer) return false;
                if (!Throttler.Throttle("接近目标位置节流")) return false;

                return Vector2.DistanceSquared(localPlayer.Position.ToVector2(), Position.ToVector2()) <= 4;
            }, $"等待接近目标位置 {Note}"),
            // 延迟
            () => t.DelayNext(Delay, $"等待 {Delay} 秒: {Note}")
        ];

    public unsafe void TargetObject()
    {
        if (FindObject() is not { } obj) return;
        
        TargetSystem.Instance()->Target = obj.ToStruct();
    }

    public IGameObject? FindObject() 
        => DService.ObjectTable.FirstOrDefault(x => x.ObjectKind == ObjectKind && x.DataId == DataID);

    public override string ToString() => 
        $"ExecutorPresetStep_{Note}_{DataID}_{Position}_{Delay}_{StopInCombat}";

    public bool Equals(ExecutorPresetStep? other)
    {
        if(ReferenceEquals(null, other)) return false;
        if(ReferenceEquals(this, other)) return true;
        
        return Note == other.Note && 
               DataID == other.DataID && 
               Position.Equals(other.Position) && 
               Delay.Equals(other.Delay) && 
               StopInCombat == other.StopInCombat;
    }

    public override bool Equals(object? obj)
    {
        if(ReferenceEquals(null, obj)) return false;
        if(ReferenceEquals(this, obj)) return true;
        if(obj.GetType() != this.GetType()) return false;
        
        return Equals((ExecutorPresetStep)obj);
    }

    public override int GetHashCode() => 
        HashCode.Combine(Note, DataID, Position, Delay, StopInCombat);
    
    public ExecutorPresetStep Copy() =>
        new()
        {
            Note         = this.Note,
            DataID       = this.DataID,
            Position     = this.Position,
            Delay        = this.Delay,
            Commands     = this.Commands,
            StopInCombat = this.StopInCombat
        };
}
