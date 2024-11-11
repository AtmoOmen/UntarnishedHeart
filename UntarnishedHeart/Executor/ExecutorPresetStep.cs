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

namespace UntarnishedHeart.Executor;

public class ExecutorPresetStep : IEquatable<ExecutorPresetStep>
{
    public string  Note         { get; set; } = string.Empty;
    public uint    DataID       { get; set; }
    public Vector3 Position     { get; set; }
    public int     Delay        { get; set; } = 5000;
    public bool    StopInCombat { get; set; } = true;

    public ExecutorStepOperationType Draw(int i, int count)
    {
        using var id = ImRaii.PushId($"Step-{i}");

        ImGui.AlignTextToFramePadding();
        ImGui.Text($"步骤 {i + 1}:");

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

        var stepName = Note;
        ImGuiOm.CompLabelLeft(
            "备注:", 200f * ImGuiHelpers.GlobalScale,
            () => ImGui.InputText("###StepNoteInput", ref stepName, 128));
        if (ImGui.IsItemDeactivatedAfterEdit())
            Note = stepName;

        var stepDataID = DataID;
        if (ImGuiOm.CompLabelLeft(
                "目标:", 200f * ImGuiHelpers.GlobalScale,
                () => ImGuiOm.InputUInt("###StepDatIDInput", ref stepDataID)))
            DataID = stepDataID;
        ImGuiOm.TooltipHover("此处应输入指定 BattleNPC 的 DataID");

        ImGui.SameLine();
        if (ImGuiOm.ButtonIcon("GetTarget", FontAwesomeIcon.Crosshairs, "取当前目标", true))
        {
            if (DService.Targets.Target is { ObjectKind: ObjectKind.BattleNpc } battleNpc)
                DataID = battleNpc.DataId;
        }

        var stepPosition = Position;
        if (ImGuiOm.CompLabelLeft(
                "位置:", 200f * ImGuiHelpers.GlobalScale,
                () => ImGui.InputFloat3("###StepPositionInput", ref stepPosition)))
            Position = stepPosition;

        ImGui.SameLine();
        if (ImGuiOm.ButtonIcon("GetPosition", FontAwesomeIcon.Bullseye, "取当前位置", true))
        {
            if (DService.ClientState.LocalPlayer is { } localPlayer)
                Position = localPlayer.Position;
        }

        var stepDelay = Delay;
        if (ImGuiOm.CompLabelLeft(
                "延迟:", 200f * ImGuiHelpers.GlobalScale,
                () =>
                {
                    var input = ImGui.InputInt("###StepDelayInput", ref stepDelay, 0, 0);
                    if (input) stepDelay = Math.Max(0, stepDelay);
                    return input;
                }))
            Delay = stepDelay;

        ImGui.SameLine();
        ImGui.Text("(ms)");

        var stepStopInCombat = StopInCombat;
        if (ImGuiOm.CompLabelLeft(
                "在战斗中停止:", 200f * ImGuiHelpers.GlobalScale,
                () => ImGui.Checkbox("###StepStopInCombatInput", ref stepStopInCombat)))
            StopInCombat = stepStopInCombat;

        return ExecutorStepOperationType.PASS;
    }

    public List<Action> GetTasks(TaskHelper t, MoveType moveType)
        =>
        [
            () => t.Enqueue(() =>
            {
                if (StopInCombat && DService.Condition[ConditionFlag.InCombat]) return false;

                switch (moveType)
                {
                    case MoveType.寻路:
                        GameFunctions.PathFindStart(Position);
                        break;
                    default:
                        GameFunctions.Teleport(Position);
                        break;
                }
                return true;
            }, "移动至目标位置"),
            () => t.Enqueue(() =>
            {
                if (DataID == 0) return true;
                if (!Throttler.Throttle("选中目标节流")) return false;

                TargetObject();
                return DService.Targets.Target != null;
            }, "选中目标"),
            () => t.Enqueue(() =>
            {
                var localPlayer = DService.ClientState.LocalPlayer;
                if (localPlayer == null) return false;
                if (!Throttler.Throttle("接近目标位置节流")) return false;

                return Vector2.DistanceSquared(localPlayer.Position.ToVector2(), Position.ToVector2()) <= 4;
            }, "等待接近目标位置"),
            () => t.DelayNext(Delay, $"等待 {Delay} 秒")
        ];

    public unsafe void TargetObject()
    {
        var obj = FindObject();
        if (obj == null) return;

        TargetSystem.Instance()->Target = obj.ToStruct();
    }

    public IGameObject? FindObject()
        => DService.ObjectTable.FirstOrDefault(x => x is { ObjectKind: ObjectKind.BattleNpc } && x.DataId == DataID);

    public override string ToString() => $"ExecutorPresetStep_{Note}_{DataID}_{Position}_{Delay}_{StopInCombat}";

    public bool Equals(ExecutorPresetStep? other)
    {
        if(ReferenceEquals(null, other)) return false;
        if(ReferenceEquals(this, other)) return true;
        return Note == other.Note && DataID == other.DataID && Position.Equals(other.Position) && Delay.Equals(other.Delay) && StopInCombat == other.StopInCombat;
    }

    public override bool Equals(object? obj)
    {
        if(ReferenceEquals(null, obj)) return false;
        if(ReferenceEquals(this, obj)) return true;
        if(obj.GetType() != this.GetType()) return false;
        return Equals((ExecutorPresetStep)obj);
    }

    public override int GetHashCode() => HashCode.Combine(Note, DataID, Position, Delay, StopInCombat);
}
