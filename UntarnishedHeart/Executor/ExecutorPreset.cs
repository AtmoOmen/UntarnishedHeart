using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Utility;
using Dalamud.Interface;
using ImGuiNET;
using System.Collections.Generic;
using System;
using System.Linq;
using UntarnishedHeart.Windows;
using System.Text;
using Newtonsoft.Json;
using System.Windows.Forms;
using Lumina.Excel.Sheets;
using UntarnishedHeart.Utils;
using Action = System.Action;

namespace UntarnishedHeart.Executor;

public class ExecutorPreset : IEquatable<ExecutorPreset>
{
    public string                   Name              { get; set; } = string.Empty;
    public ushort                   Zone              { get; set; }
    public List<ExecutorPresetStep> Steps             { get; set; } = [];
    public bool                     AutoOpenTreasures { get; set; }
    public int                      DutyDelay         { get; set; } = 500;

    public bool IsValid => Zone != 0 && Steps.Count > 0 && Main.ZonePlaceNames.ContainsKey(Zone);
    
    private string contentZoneSearchInput = string.Empty;

    public void Draw()
    {
        var name = Name;
        if (ImGuiOm.CompLabelLeft(
                "名称:", 200f * ImGuiHelpers.GlobalScale,
                () => ImGui.InputText("###PresetNameInput", ref name, 128)))
            Name = name;

        using (ImRaii.Group())
        {
            ImGui.AlignTextToFramePadding();
            ImGui.Text("区域:");
        
            var zone = (uint)Zone;
            ImGui.SameLine();
            ImGui.SetNextItemWidth(200f * ImGuiHelpers.GlobalScale);
            if (ContentSelectCombo(ref zone, ref contentZoneSearchInput))
                Zone = (ushort)zone;

            ImGui.SameLine();
            if (ImGuiOm.ButtonIcon("GetZone", FontAwesomeIcon.MapMarkedAlt, "取当前区域", true))
                Zone = DService.ClientState.TerritoryType;

            using (ImRaii.PushIndent())
            {
                if (LuminaGetter.TryGetRow<TerritoryType>(Zone, out var zoneData))
                {
                    var zoneName    = zoneData.PlaceName.Value.Name.ExtractText()              ?? "未知区域";
                    var contentName = zoneData.ContentFinderCondition.Value.Name.ExtractText() ?? "未知副本";
                
                    ImGui.Text($"({zoneName} / {contentName})");
                }
            }
        }

        using (ImRaii.Group())
        {
            var delay = DutyDelay;
            if (ImGuiOm.CompLabelLeft(
                    "延迟:", 200f * ImGuiHelpers.GlobalScale,
                    () => ImGui.InputInt("###PresetLeaveDutyDelayInput", ref delay, 0, 0)))
                DutyDelay = Math.Max(0, delay);
            
            ImGui.SameLine();
            ImGui.Text("(ms)");
        }
        ImGuiOm.TooltipHover("完成副本后, 在退出副本前需要等待的时间");
        
        ImGui.Spacing();
        
        var autoOpenTreasure = AutoOpenTreasures;
        if (ImGui.Checkbox("副本结束时, 自动开启宝箱", ref autoOpenTreasure))
            AutoOpenTreasures = autoOpenTreasure;
        ImGuiOm.HelpMarker("请确保本副本的确有宝箱, 否则流程将卡死", 20f, FontAwesomeIcon.InfoCircle, true);

        ImGui.Dummy(new(8f));

        if (ImGuiOm.ButtonIconWithText(FontAwesomeIcon.Plus, "添加新步骤", true))
            Steps.Add(new());
        ImGuiOm.HelpMarker("每一步骤内的各判断, 都遵循界面绘制顺序, 从上到下依次判断执行", useStaticFont: true);

        ImGui.Spacing();
        
        for (var i = 0; i < Steps.Count; i++)
        {
            var step = Steps[i];
            
            using var node = ImRaii.TreeNode($"第 {i + 1} 步: {step.Note}");
            if (!node) continue;
            
            var ret = step.Draw(i, Steps.Count);
            Action executorOperationAction = ret switch
            {
                ExecutorStepOperationType.DELETE   => () => Steps.RemoveAt(i),
                ExecutorStepOperationType.MOVEDOWN => () => Steps.Swap(i, i + 1),
                ExecutorStepOperationType.MOVEUP   => () => Steps.Swap(i, i - 1),
                ExecutorStepOperationType.COPY     => () => Steps.Insert(i  + 1, step.Copy()),
                ExecutorStepOperationType.PASS     => () => { },
                _                                  => () => { }
            };
            executorOperationAction();
        }
    }

    public List<Action> GetTasks(TaskHelper t)
        => Steps.SelectMany(x => x.GetTasks(t)).ToList();

    public override string ToString() => $"ExecutorPreset_{Name}_{Zone}_{Steps.Count}Steps";

    public bool Equals(ExecutorPreset? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;

        return Name == other.Name && Zone == other.Zone && Steps.SequenceEqual(other.Steps);
    }

    public void ExportToClipboard()
    {
        try
        {
            var json = JsonConvert.SerializeObject(this);
            var base64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(json));
            Clipboard.SetText(base64);
            NotifyHelper.NotificationSuccess("已成功导出预设至剪贴板");
        }
        catch (Exception)
        {
            NotifyHelper.NotificationError("尝试导出预设至剪贴板时发生错误");
        }
    }

    public static ExecutorPreset? ImportFromClipboard()
    {
        try
        {
            var base64 = Clipboard.GetText();
            if (!string.IsNullOrEmpty(base64))
            {
                var json = Encoding.UTF8.GetString(Convert.FromBase64String(base64));

                var config = JsonConvert.DeserializeObject<ExecutorPreset>(json);
                if (config != null)
                    NotifyHelper.NotificationSuccess("已成功从剪贴板导入预设");
                return config;
            }
        }
        catch (Exception)
        {
            NotifyHelper.NotificationError("尝试从剪贴板导入预设时发生错误");
        }
        return null;
    }

    public override bool Equals(object? obj) => Equals(obj as ExecutorPreset);

    public override int GetHashCode() => HashCode.Combine(Name, Zone, Steps);
}

