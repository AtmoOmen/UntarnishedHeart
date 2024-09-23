using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using FFXIVClientStructs.FFXIV.Client.Game.Event;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using ImGuiNET;
using Lumina.Excel.GeneratedSheets;
using UntarnishedHeart.Managers;
using UntarnishedHeart.Utils;

namespace UntarnishedHeart.Windows;

public class Main() : Window($"{PluginName} 主界面###{PluginName}-MainWindow", ImGuiWindowFlags.AlwaysAutoResize), IDisposable
{
    private static readonly Executor Executor = new();

    private static int SelectedPresetIndex;
    private static bool IsSelectorDisplay;

    public static readonly Dictionary<uint, string> ZonePlaceNames;

    static Main()
    {
        ZonePlaceNames = LuminaCache.Get<TerritoryType>()
                                    .Select(x => (x.RowId, x?.ExtractPlaceName()))
                                    .Where(x => !string.IsNullOrWhiteSpace(x.Item2))
                                    .ToDictionary(x => x.RowId, x => x.Item2)!;

        Executor.Init();
    }

    public override void Draw()
    {
        if (SelectedPresetIndex >= Service.Config.Presets.Count || SelectedPresetIndex < 0)
            SelectedPresetIndex = 0;

        if (Service.Config.Presets.Count == 0)
        {
            Service.Config.Presets.Add(new()
            {
                Name = "O5 魔列车", Zone = 748, Steps = [new() { DataID = 8510, Note = "魔列车", Position = new(0, 0, -15) }]
            });
            Service.Config.Save();
        }

        DrawExecutorInfo();

        ImGui.Separator();
        ImGui.Spacing();

        DrawNesscaryInfo();

        ImGui.Separator();
        ImGui.Spacing();

        DrawExecutorConfig();

        ImGui.Separator();
        ImGui.Spacing();

        using (ImRaii.Disabled(Executor.IsRunning))
        {
            if (ImGuiOm.ButtonSelectable("开始"))
                Executor.Start(
                    SelectedPresetIndex > Service.Config.Presets.Count - 1
                        ? null
                        : Service.Config.Presets[SelectedPresetIndex], Service.Config.RunTimes);

            if (Service.Config.LeaderMode)
                ImGuiOm.TooltipHover("你已开启队长模式, 请确认好要刷取的副本已在任务搜索器内选取完毕, 且相关设置已经配置完成");
        }

        if (ImGuiOm.ButtonSelectable("结束"))
            Executor.Stop();

        if (!IsSelectorDisplay) return;

        var windowWidth = ImGui.GetWindowWidth();
        var windowPos = ImGui.GetWindowPos();
        ImGui.SetNextWindowPos(windowPos with { X = windowPos.X + windowWidth });
        if (ImGui.Begin($"预设选择器###{PluginName}-PresetSelector", ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.AlwaysAutoResize))
        {
            ImGui.AlignTextToFramePadding();
            ImGui.Text("选择预设:");

            ImGui.SameLine();
            ImGui.SetNextItemWidth(150f * ImGuiHelpers.GlobalScale);
            var selectedPreset = Service.Config.Presets[SelectedPresetIndex];
            using (var combo = ImRaii.Combo("###PresetSelectCombo", $"{selectedPreset.Name}", ImGuiComboFlags.HeightLarge))
            {
                if (combo)
                {
                    for (var i = 0; i < Service.Config.Presets.Count; i++)
                    {
                        var preset = Service.Config.Presets[i];
                        if (ImGui.Selectable($"{preset.Name}###{preset}-{i}"))
                            SelectedPresetIndex = i;

                        using var popup = ImRaii.ContextPopup($"{preset}-{i}ContextPopup");
                        if (popup)
                        {
                            using (ImRaii.Disabled(Service.Config.Presets.Count == 1))
                            {
                                if (ImGui.MenuItem($"删除##{preset}-{i}"))
                                    Service.Config.Presets.Remove(preset);
                            }
                        }
                    }
                }
            }

            ImGui.SameLine();
            if (ImGuiOm.ButtonIcon("AddNewPreset", FontAwesomeIcon.FileCirclePlus, "添加新预设", true))
            {
                Service.Config.Presets.Add(new());
                SelectedPresetIndex = Service.Config.Presets.Count - 1;
            }

            ImGui.Separator();
            ImGui.Spacing();

            selectedPreset.Draw();

            ImGui.End();
        }
    }

    private static void DrawExecutorInfo()
    {
        ImGui.TextColored(LightBlue, "运行状态:");
        using var indent = ImRaii.PushIndent();

        ImGui.Text("当前状态:");

        ImGui.SameLine();
        ImGui.TextColored(!Executor.IsRunning ? ImGuiColors.DalamudRed : ImGuiColors.ParsedGreen,
                          !Executor.IsRunning ? "等待中" : "运行中");

        ImGui.SameLine();
        ImGui.TextDisabled("|");

        ImGui.SameLine();
        ImGui.Text("次数:");

        ImGui.SameLine();
        ImGui.Text($"{Executor.CurrentRound} / {Executor.MaxRound}");

        ImGui.Text("运行信息:");

        ImGui.SameLine();
        ImGui.Text($"{Executor.RunningMessage}");
    }

    private static void DrawNesscaryInfo()
    {
        ImGui.TextColored(LightBlue, "必要信息:");
        using var indent = ImRaii.PushIndent();

        ImGui.Text("当前区域:");

        var zoneName = ZonePlaceNames.GetValueOrDefault(DService.ClientState.TerritoryType, "未知区域");
        ImGui.SameLine();
        ImGui.Text($"{zoneName} ({DService.ClientState.TerritoryType})");
        ImGui.Text("当前目标:");

        var target = DService.Targets.Target;
        ImGui.SameLine();
        ImGui.Text(target is not { ObjectKind:ObjectKind.BattleNpc } ? string.Empty : $"{target.Name} (DataID: {target.DataId})");

        ImGui.Text("当前位置:");

        ImGui.SameLine();
        ImGui.Text($"{DService.ClientState.LocalPlayer?.Position:F2}");
    }

    private static void DrawExecutorConfig()
    {
        ImGui.TextColored(LightBlue, "运行设置:");
        using var indent = ImRaii.PushIndent();

        using (ImRaii.Group())
        {
            ImGui.AlignTextToFramePadding();
            ImGui.Text("移动方式:");

            foreach (var moveType in Enum.GetValues<MoveType>())
            {
                ImGui.SameLine();
                if (ImGui.RadioButton(moveType.ToString(), moveType == Service.Config.MoveType))
                {
                    Service.Config.MoveType = moveType;
                    Service.Config.Save();
                }
            }

            var runTimes = Service.Config.RunTimes;
            if (ImGuiOm.CompLabelLeft("运行次数:", 50f * ImGuiHelpers.GlobalScale,
                                      () => ImGui.InputInt("###", ref runTimes, 0, 0)))
            {
                Service.Config.RunTimes = runTimes;
                Service.Config.Save();
            }

            ImGui.SameLine();
            var isLeaderMode = Service.Config.LeaderMode;
            if (ImGui.Checkbox("队长模式", ref isLeaderMode))
            {
                Service.Config.LeaderMode = isLeaderMode;
                Service.Config.Save();
            }

            ImGuiOm.HelpMarker("启用队长模式后, 意味着副本结束后会自动尝试排入同一副本", 20f, FontAwesomeIcon.InfoCircle, true);
        }
        
        ImGui.SameLine();
        if (ImGuiOm.ButtonIconWithTextVertical(FontAwesomeIcon.Eye, "选择预设", true))
            IsSelectorDisplay ^= true;
    }

    public void Dispose()
    {
        Executor.Uninit();
    }
}

public class Executor
{
    public bool            IsRunning      { get; private set; }
    public uint            CurrentRound   { get; private set; }
    public int             MaxRound       { get; set; }
    public string          RunningMessage => TaskHelper.CurrentTaskName;
    public ExecutorPreset? ExecutorPreset { get; set; }

    public TaskHelper? TaskHelper { get; private set; }

    public void Init()
    {
        TaskHelper ??= new() { TimeLimitMS = int.MaxValue };

        DService.ClientState.TerritoryChanged += OnZoneChanged;
        DService.DutyState.DutyCompleted += OnDutyCompleted;
        DService.AddonLifecycle.RegisterListener(AddonEvent.PostSetup, "ContentsFinderConfirm", OnAddonSetup);
    }

    public void Start(ExecutorPreset? preset, int maxRound = -1)
    {
        if (preset is not { IsValid: true })
        {
            IsRunning = false;
            return;
        }

        if (IsRunning) return;

        IsRunning = true;
        CurrentRound = 0;
        MaxRound = maxRound;
        ExecutorPreset = preset;
        OnZoneChanged(DService.ClientState.TerritoryType);
    }

    public void Stop()
    {
        IsRunning = false;
        TaskHelper?.Abort();
    }

    private unsafe void OnAddonSetup(AddonEvent type, AddonArgs args)
    {
        if (!IsRunning || args.Addon == nint.Zero) return;
        var button = ((AddonContentsFinderConfirm*)args.Addon)->CommenceButton;
        button->ClickAddonButton((AtkComponentBase*)args.Addon, 8);
    }

    private void OnZoneChanged(ushort zone)
    {
        if (ExecutorPreset == null || zone != ExecutorPreset.Zone || !IsRunning) return;
        EnqueueTPTasks();
    }

    private unsafe void EnqueueTPTasks()
    {
        TaskHelper.Enqueue(() => IsScreenReady(), "等待区域切换加载结束");

        TaskHelper.Enqueue(() =>
        {
            var framework = EventFramework.Instance();
            if (framework == null) return false;
            var director = framework->GetContentDirector();
            if (director == null) return false;
            return ((int)director->ContentTimeLeft & 100) > 1;
        }, "等待副本开始");

        var counter = 0;
        foreach (var task in ExecutorPreset.GetTasks(Service.Config.MoveType))
        {
            TaskHelper.Enqueue(task, $"运行预设步骤: {counter}");
            counter++;
        }
    }

    private void OnDutyCompleted(object? sender, ushort zone)
    {
        if (ExecutorPreset == null || zone != ExecutorPreset.Zone || !IsRunning) return;

        GameFunctions.LeaveDuty();
        CurrentRound++;

        if (MaxRound != -1 && CurrentRound >= MaxRound)
        {
            Stop();
            return;
        }

        TaskHelper?.Abort();
        if (!Service.Config.LeaderMode) return;
        EnqueueRegDuty();
    }

    private unsafe void EnqueueRegDuty()
    {
        TaskHelper.Enqueue(() => EventFramework.Instance()->GetContentDirector() == null, "等待副本结束");
        TaskHelper.Enqueue(() => IsScreenReady(), "等待区域切换加载结束");
        TaskHelper.Enqueue(() =>
        {
            if (!Throttler.Throttle("进入副本节流")) return false;
            GameFunctions.RegisterToEnterDuty();
            return DService.Condition[ConditionFlag.WaitingForDutyFinder] || DService.Condition[ConditionFlag.WaitingForDuty];
        }, "等待进入下一局");
    }

    public void Uninit()
    {
        DService.ClientState.TerritoryChanged -= OnZoneChanged;
        DService.DutyState.DutyCompleted -= OnDutyCompleted;
        DService.AddonLifecycle.UnregisterListener(OnAddonSetup);

        TaskHelper?.Abort();
        TaskHelper = null;

        IsRunning = false;
    }
}

public class ExecutorPreset : IEquatable<ExecutorPreset>
{
    public string                   Name  { get; set; } = string.Empty;
    public ushort                   Zone  { get; set; }
    public List<ExecutorPresetStep> Steps { get; set; } = [];

    public bool IsValid => Zone != 0 && Steps.Count > 0 && Main.ZonePlaceNames.ContainsKey(Zone);

    public void Draw()
    {
        var name = Name;
        if (ImGuiOm.CompLabelLeft(
                "名称:", 200f * ImGuiHelpers.GlobalScale,
                () => ImGui.InputText("###PresetNameInput", ref name, 128)))
            Name = name;

        var zone = (int)Zone;
        if (ImGuiOm.CompLabelLeft(
                "区域:", 200f * ImGuiHelpers.GlobalScale,
                () => ImGui.InputInt("###PresetZoneInput", ref zone, 0, 0)))
            Zone = (ushort)Math.Clamp(zone, 0, ushort.MaxValue);

        ImGui.SameLine();
        if (ImGuiOm.ButtonIcon("GetZone", FontAwesomeIcon.MapMarkedAlt, "取当前区域", true))
            Zone = DService.ClientState.TerritoryType;

        using (ImRaii.PushIndent())
        {
            var zoneName = Main.ZonePlaceNames.GetValueOrDefault(Zone, "未知区域");
            ImGui.Text($"({zoneName})");
        }

        ImGui.Dummy(new(8f));

        if (ImGuiOm.ButtonIconWithText(FontAwesomeIcon.Plus, "添加新步骤", true))
            Steps.Add(new());

        for (var i = 0; i < Steps.Count; i++)
        {
            var step = Steps[i];
            if (step.Draw(i)) Steps.RemoveAt(i);
        }
    }

    public List<Func<bool?>> GetTasks(MoveType moveType)
        => Steps.SelectMany(x => x.GetTasks(moveType)).ToList();

    public override string ToString() => $"ExecutorPreset_{Name}_{Zone}_{Steps.Count}Steps";

    public bool Equals(ExecutorPreset? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;

        return Name == other.Name && Zone == other.Zone && Steps.SequenceEqual(other.Steps);
    }

    public override bool Equals(object? obj) => Equals(obj as ExecutorPreset);

    public override int GetHashCode() => HashCode.Combine(Name, Zone, Steps);
}

public class ExecutorPresetStep : IEquatable<ExecutorPresetStep>
{
    public string  Note     { get; set; } = string.Empty;
    public uint    DataID   { get; set; }
    public Vector3 Position { get; set; }

    public bool Draw(int i)
    {
        using var id = ImRaii.PushId($"{this}-{i}");

        ImGui.AlignTextToFramePadding();
        ImGui.Text($"步骤 {i + 1}:");

        ImGui.SameLine();
        if (ImGuiOm.ButtonIconWithText(FontAwesomeIcon.TrashAlt, "删除本步", true))
            return true;

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
                () => ImGui.InputFloat3("###StepDatIDInput", ref stepPosition)))
            Position = stepPosition;

        ImGui.SameLine();
        if (ImGuiOm.ButtonIcon("GetPosition", FontAwesomeIcon.Bullseye, "取当前位置", true))
        {
            if (DService.ClientState.LocalPlayer is { } localPlayer)
                Position = localPlayer.Position;
        }

        return false;
    }

    public List<Func<bool?>> GetTasks(MoveType moveType)
        =>
        [
            () =>
            {
                if (DService.Condition[ConditionFlag.InCombat]) return false;

                var obj = FindObject();
                switch (moveType)
                {
                    case MoveType.寻路 when obj != null:
                        GameFunctions.Move(obj.GameObjectId);
                        break;
                    default:
                        GameFunctions.Teleport(Position);
                        break;
                }

                return true;
            },
            () =>
            {
                if (DService.Condition[ConditionFlag.InCombat]) return false;
                if (DataID == 0) return true;
                TargetObject();
                return DService.Targets.Target != null;
            },
        ];

    public unsafe void TargetObject()
    {
        var obj = FindObject();
        if (obj == null) return;

        TargetSystem.Instance()->Target = obj.ToStruct();
    }

    public IGameObject? FindObject()
        => DService.ObjectTable.FirstOrDefault(x => x is { ObjectKind: ObjectKind.BattleNpc } && x.DataId == DataID);

    public override string ToString() => $"ExecutorPresetStep_{Note}_{DataID}_{Position}";

    public bool Equals(ExecutorPresetStep? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;

        return Note == other.Note && DataID == other.DataID && Position.Equals(other.Position);
    }

    public override bool Equals(object? obj) => Equals(obj as ExecutorPresetStep);

    public override int GetHashCode() => HashCode.Combine(Note, DataID, Position);
}

public enum MoveType
{
    寻路,
    传送
}
