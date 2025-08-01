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
using Dalamud.Game.ClientState.Objects.Enums;
using OmenTools.Service;
using UntarnishedHeart.Utils;
using UntarnishedHeart.Managers;

namespace UntarnishedHeart.Executor;

public class ExecutorPresetStep : IEquatable<ExecutorPresetStep>
{
    /// <summary>
    /// 步骤名称
    /// </summary>
    public string Note { get; set; } = string.Empty;

    /// <summary>
    /// 若为忙碌状态, 则等待
    /// </summary>
    public bool StopWhenBusy { get; set; }

    /// <summary>
    /// 若为战斗状态, 则等待
    /// </summary>
    public bool StopInCombat { get; set; } = true;

    /// <summary>
    /// 若任一队友不在无法战斗状态, 则等待
    /// </summary>
    public bool StopWhenAnyAlive { get; set; }

    public Vector3          Position           { get; set; }
    public MoveType         MoveType           { get; set; } = MoveType.无;
    public bool             WaitForGetClose    { get; set; }
    public uint             DataID             { get; set; }
    public ObjectKind       ObjectKind         { get; set; } = ObjectKind.BattleNpc;
    public bool             WaitForTarget      { get; set; } = true;
    public bool             InteractWithTarget { get; set; }
    public string           Commands           { get; set; } = string.Empty;
    public CommandCondition CommandCondition   { get; set; } = new();
    public uint             Delay              { get; set; } = 5000;

    public void Draw(ref int i, List<ExecutorPresetStep> steps)
    {
        using var id = ImRaii.PushId($"Step-{i}");
        using var group = ImRaii.Group();

        var stepName = Note;
        ImGuiOm.CompLabelLeft(
            "备注:", ImGui.GetContentRegionAvail().X - ImGui.GetStyle().ItemSpacing.X,
            () => ImGui.InputText("###StepNoteInput", ref stepName, 128));
        if (ImGui.IsItemDeactivatedAfterEdit())
            Note = stepName;

        ImGui.Separator();
        ImGui.Spacing();

        using (var child = ImRaii.Child("StepContentChild", ImGui.GetContentRegionAvail() - ImGui.GetStyle().ItemSpacing))
        {
            if (child)
            {
                using var tabBar = ImRaii.TabBar("###StepContentTabBar");
                if (!tabBar) return;

                using (var preWait = ImRaii.TabItem("前置等待"))
                {
                    if (preWait)
                    {
                        var stepStopWhenAnyAlive = StopWhenAnyAlive;
                        using (ImRaii.PushColor(ImGuiCol.Text, LightSkyBlue))
                        {
                            if (ImGui.Checkbox("若任一队友不在无法战斗状态, 则等待###StepStopWhenAnyAliveInput", ref stepStopWhenAnyAlive))
                                StopWhenAnyAlive = stepStopWhenAnyAlive;
                        }

                        ImGuiOm.TooltipHover("若勾选, 则在执行此步时检查是否存在任一队友不在无法战斗状态, 若存在, 则阻塞步骤执行");

                        var stepStopInCombat = StopInCombat;
                        using (ImRaii.PushColor(ImGuiCol.Text, LightSkyBlue))
                        {
                            if (ImGui.Checkbox("若已进入战斗, 则等待###StepStopInCombatInput", ref stepStopInCombat))
                                StopInCombat = stepStopInCombat;
                        }

                        ImGuiOm.TooltipHover("若勾选, 则在执行此步时检查是否进入战斗状态, 若已进入, 则阻塞步骤执行");

                        var stepStopWhenBusy = StopWhenBusy;
                        using (ImRaii.PushColor(ImGuiCol.Text, LightSkyBlue))
                        {
                            if (ImGui.Checkbox("若为忙碌状态, 则等待###StepStopWhenBusyInput", ref stepStopWhenBusy))
                                StopWhenBusy = stepStopWhenBusy;
                        }

                        ImGuiOm.TooltipHover("若勾选, 则在执行此步时检查是否正处于忙碌状态 (如: 过图加载, 交互等), 若已进入, 则阻塞步骤执行");
                    }
                    else
                        ImGuiOm.TooltipHover("各类状态检查, 每一步骤开始时顺序执行此处每一项状态检查");
                }

                using (var positionCheck = ImRaii.TabItem("坐标"))
                {
                    if (positionCheck)
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
                                if (DService.ObjectTable.LocalPlayer is { } localPlayer)
                                    Position = localPlayer.Position;
                            }

                            ImGui.SameLine();
                            if (ImGuiOm.ButtonIcon("TPToThis", FontAwesomeIcon.Flag, "传送到此位置", true))
                                GameFunctions.Teleport(stepPosition);
                        }

                        ImGui.Spacing();
                        
                        using (ImRaii.Group())
                        {
                            var waitForGetClose = WaitForGetClose;
                            if (ImGui.Checkbox("等待接近坐标后再继续###WaitForGetCloseInput", ref waitForGetClose))
                                WaitForGetClose = waitForGetClose;
                            ImGuiOm.TooltipHover("若勾选, 则会等待完全接近坐标后再继续执行下面的操作");
                        }
                    }
                    else
                        ImGuiOm.TooltipHover("在执行完左侧的前置等待后, 会按照此处的配置移动到指定的坐标");
                }

                using (var targetCheck = ImRaii.TabItem("目标"))
                {
                    if (targetCheck)
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
                        ImGuiOm.TooltipHover("在开始向左侧配置的坐标移动后, 会按照此处的配置选中指定的目标");
                }

                using (var textCommand = ImRaii.TabItem("文本指令"))
                {
                    if (textCommand)
                    {
                        var stepCommands = Commands;
                        var commandInputHeight = Math.Max(ImGui.GetTextLineHeightWithSpacing() * 3f,
                                                          ImGui.CalcTextSize(stepCommands).Y + (2 * ImGui.GetStyle().ItemSpacing.Y));
                        
                        if (ImGui.InputTextMultiline("###CommandsInput", ref stepCommands, 1024,
                                                     new(ImGui.GetContentRegionAvail().X - ImGui.GetStyle().ItemSpacing.X, commandInputHeight)))
                            Commands = stepCommands;
                        ImGuiOm.TooltipHover("支持以下特殊指令:\n" +
                                             "/wait <时间(ms)> - 等待指定毫秒的时间 (如: /wait 2000 - 等待 2 秒)");
                        
                        CommandCondition.Draw();
                    }
                    else
                        ImGuiOm.TooltipHover("在执行完左侧的目标选择后, 会按照此处的配置选中自定义文本指令\n" +
                                             "支持多条文本指令, 一行一条");
                }

                using (var postWait = ImRaii.TabItem("后置等待")) 
                {
                    if (postWait)
                    {
                        using (ImRaii.Group())
                        {
                            var stepDelay = Delay;
                            if (ImGuiOm.CompLabelLeft(
                                    "等待:", 200f * ImGuiHelpers.GlobalScale,
                                    () => ImGuiOm.InputUInt("(ms)###StepDelayInput", ref stepDelay, 0, 0)))
                                Delay = stepDelay;
                            ImGuiOm.TooltipHover("在开始下一步骤前, 需要等待的时间");
                        }
                    }
                }

                if (i > 0)
                {
                    if (ImGui.TabItemButton("↑"))
                    {
                        var index = i - 1;
                        steps.Swap(i, index);
                        i = index;
                    }
                    ImGuiOm.TooltipHover("上移");
                }

                if (i < steps.Count - 1)
                {
                    if (ImGui.TabItemButton("↓"))
                    {
                        var index = i + 1;
                        steps.Swap(i, index);
                        i = index;
                    }
                    ImGuiOm.TooltipHover("下移");
                }
            }
        }
    }

    public unsafe List<Action> GetTasks(TaskHelper t)
    {
        return
        [
            // 检查队友存活状态
            () => t.Enqueue(() =>
                            {
                                if (!StopWhenAnyAlive || DService.PartyList.Length < 2) return true;
                                if (DService.ObjectTable.LocalPlayer is not { } localPlayer) return false;

                                foreach (var member in DService.PartyList)
                                {
                                    if (member.ObjectId  == localPlayer.ToStruct()->EntityId) continue;
                                    if (member.CurrentHP != 0) return false;
                                }

                                return true;
                            }, $"检查队友存活状态: {Note}"),
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

                                if (DService.ObjectTable.LocalPlayer is not { } localPlayer) return false;
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
                // 没条件
                if (CommandCondition.Conditions.Count == 0)
                    EnqueueTextCommands();
                else
                {
                    t.Enqueue(() => Control.GetLocalPlayer() != null, $"执行文本指令时本地玩家不能为空: {Note}");
                    t.Enqueue(() =>
                    {
                        switch (CommandCondition.ExecuteType)
                        {
                            case CommandExecuteType.Pass:
                                if (!CommandCondition.IsConditionsTrue()) break;
                                EnqueueTextCommands(2);
                                break;
                            case CommandExecuteType.Wait:
                                t.Enqueue(() => CommandCondition.IsConditionsTrue(), $"等待文本指令条件变成真: {Note}", weight: 2);
                                t.Enqueue(() => EnqueueTextCommands(2), $"文本指令条件已为真, 入队指令执行: {Note}", weight: 2);
                                break;
                            case CommandExecuteType.Repeat:
                                // 已满足条件只执行一次
                                if (CommandCondition.IsConditionsTrue())
                                {
                                    EnqueueTextCommands(2);
                                    break;
                                }
                                
                                EnqueueTextCommandsRepeat();
                                break;
                        }
                    }, $"入队文本指令条件检查任务群: {Note}");
                }
            },
            // 延迟
            () => t.DelayNext((int)Delay, $"等待 {Delay} 秒: {Note}")
        ];

        void EnqueueTextCommands(int weight = 0)
        {
            foreach (var command in Commands.Split('\n'))
            {
                if (command.StartsWith("/wait"))
                {
                    var spilted = command.Split(' ');
                    if (spilted.Length != 2 || !uint.TryParse(spilted[1], out var waitTime)) goto SpecialCommandHandle;
                    t.DelayNext((int)waitTime, $"特殊文本指令 {command}: {Note}", weight: weight);
                    continue;
                }
                
                SpecialCommandHandle:
                t.Enqueue(() => ChatHelper.SendMessage(command), $"使用文本指令: {Note} {command}", weight: weight);
                t.DelayNext(100, $"使用文本指令节流: {Note} {command}", weight: weight);
            }
        }

        void EnqueueTextCommandsRepeat()
        {
            if (CommandCondition.IsConditionsTrue()) return;
            t.Enqueue(() => EnqueueTextCommands(2), $"文本指令不为真, 入队指令执行: {Note}", weight: 2);
            if (CommandCondition.TimeValue > 0)
                t.DelayNext((int)CommandCondition.TimeValue, $"文本指令完成, 等待: {Note}", weight: 2);
            t.Enqueue(EnqueueTextCommandsRepeat, "开始新一轮重复检查", weight: 2);
        }
    }

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

    public static ExecutorPresetStep Copy(ExecutorPresetStep source) =>
        new()
        {
            Note               = source.Note,
            StopInCombat       = source.StopInCombat,
            StopWhenBusy       = source.StopWhenBusy,
            StopWhenAnyAlive   = source.StopWhenAnyAlive,
            Position           = source.Position,
            MoveType           = source.MoveType,
            WaitForGetClose    = source.WaitForGetClose,
            DataID             = source.DataID,
            ObjectKind         = source.ObjectKind,
            WaitForTarget      = source.WaitForTarget,
            InteractWithTarget = source.InteractWithTarget,
            Commands           = source.Commands,
            CommandCondition   = CommandCondition.Copy(source.CommandCondition),
            Delay              = source.Delay,
        };
}
