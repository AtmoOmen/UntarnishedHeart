using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Utility;
using Dalamud.Interface;
using Dalamud.Bindings.ImGui;
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
    public string                   Remark            { get; set; } = string.Empty;
    public ushort                   Zone              { get; set; }
    public List<ExecutorPresetStep> Steps             { get; set; } = [];
    public bool                     AutoOpenTreasures { get; set; }
    public int                      DutyDelay         { get; set; } = 500;

    public bool IsValid => Zone != 0 && Steps.Count > 0 && Main.ZonePlaceNames.ContainsKey(Zone);

    private ExecutorPresetStep? StepToCopy;

    private string ZoneSearchInput = string.Empty;
    
    private int CurrentStep = -1;

    public void Draw()
    {
        using var tabBar = ImRaii.TabBar("###ExecutorPresetEditor");
        if (!tabBar) return;

        using (var basicInfo = ImRaii.TabItem("基本信息"))
        {
            if (basicInfo)
            {
                DrawBasicInfo();
            }
        }

        using (var stepInfo = ImRaii.TabItem("步骤"))
        {
            if (stepInfo)
            {
                DrawStepInfo();
            }
        }
    }

    private void DrawBasicInfo()
    {
        var name = Name;
        if (ImGuiOm.CompLabelLeft("名称:", -1f, () => ImGui.InputText("###PresetNameInput", ref name, 128)))
            Name = name;
        
        ImGui.NewLine();

        using (ImRaii.Group())
        {
            ImGui.AlignTextToFramePadding();
            ImGui.Text("副本区域:");

            var zone = (uint)Zone;
            ImGui.SameLine();
            ImGui.SetNextItemWidth(350f * ImGuiHelpers.GlobalScale);
            if (ContentSelectCombo(ref zone, ref ZoneSearchInput))
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

        var delay = DutyDelay;
        if (ImGuiOm.CompLabelLeft(
                "退出延迟:", 350f * ImGuiHelpers.GlobalScale,
                () => ImGui.InputInt("(ms)###PresetLeaveDutyDelayInput", ref delay, 0, 0)))
            DutyDelay = Math.Max(0, delay);
        ImGuiOm.TooltipHover("完成副本后, 在退出副本前需要等待的时间");

        ImGui.Spacing();

        var autoOpenTreasure = AutoOpenTreasures;
        if (ImGui.Checkbox("副本结束时, 自动开启宝箱", ref autoOpenTreasure))
            AutoOpenTreasures = autoOpenTreasure;
        ImGuiOm.HelpMarker("请确保本副本的确有宝箱, 否则流程将卡死", 20f, FontAwesomeIcon.InfoCircle, true);

        ImGui.NewLine();

        ImGui.Text("备注:");
        var remark = Remark;
        if (ImGui.InputTextMultiline("###RemarkInput", ref remark, 2056,
                                     ImGui.GetContentRegionAvail() - ImGui.GetStyle().ItemSpacing))
            Remark = remark;
    }

    private unsafe void DrawStepInfo()
    {
        if (Steps.Count == 0)
            CurrentStep = -1;
        else if (CurrentStep >= Steps.Count)
            CurrentStep = Steps.Count - 1;

        var availableContent  = ImGui.GetContentRegionAvail();

        var leftChildWidth   = Math.Min(175f * ImGuiHelpers.GlobalScale, availableContent.X * 0.3f);
        var rightChildHeight = availableContent.X - leftChildWidth - ImGui.GetStyle().ItemSpacing.X;
        var childHeight      = availableContent.Y - ImGui.GetStyle().ItemSpacing.Y;

        using (var child = ImRaii.Child("StepsSelectChild", new(leftChildWidth, childHeight)))
        {
            if (child)
            {
                if (ImGuiOm.ButtonSelectable("添加新步骤"))
                    Steps.Add(new());
                ImGuiOm.TooltipHover("每一步骤内的各判断, 都遵循界面绘制顺序, 从上到下、从左到右依次判断执行");

                ImGui.Separator();
                ImGui.Spacing();

                for (var i = 0; i < Steps.Count; i++)
                {
                    var step = Steps[i];

                    var stepName = $"{i + 1}. {step.Note}" + (step.Delay > 0 ? $" ({(float)step.Delay / 1000:F2}s)" : string.Empty);
                    
                    // 拖拽源
                    if (ImGui.Selectable(stepName, i == CurrentStep))
                        CurrentStep = i;
                    
                    // 开始拖拽
                    if (ImGui.BeginDragDropSource(ImGuiDragDropFlags.None))
                    {
                        var dragIndex = i;
                        ImGui.SetDragDropPayload("STEP_REORDER", BitConverter.GetBytes(dragIndex));
                        ImGui.Text($"步骤: {stepName}");
                        ImGui.EndDragDropSource();
                    }
                    
                    // 拖拽目标
                    if (ImGui.BeginDragDropTarget())
                    {
                        var payload = ImGui.AcceptDragDropPayload("STEP_REORDER");
                        if (payload.Data != null)
                        {
                            var sourceIndex = *(int*)payload.Data;
                            if (sourceIndex != i && sourceIndex >= 0 && sourceIndex < Steps.Count)
                            {
                                // 执行拖拽排序 - 直接交换两个步骤的位置
                                (Steps[sourceIndex], Steps[i]) = (Steps[i], Steps[sourceIndex]);

                                // 更新当前选中步骤
                                if (CurrentStep == sourceIndex)
                                    CurrentStep = i;
                                else if (CurrentStep == i)
                                    CurrentStep = sourceIndex;
                            }
                        }

                        ImGui.EndDragDropTarget();
                    }
                    
                    ImGuiOm.TooltipHover(stepName);
                    DrawStepContextMenu(i, step);
                }
            }
        }

        ImGui.SameLine();
        using (var child = ImRaii.Child("StepsDrawChild", new(rightChildHeight, childHeight)))
        {
            if (child)
            {
                if (CurrentStep == -1) return;

                var step = Steps[CurrentStep];

                step.Draw(ref CurrentStep, Steps);
            }
        }

        return;

        void DrawStepContextMenu(int i, ExecutorPresetStep step)
        {
            var contextOperation = StepOperationType.Pass;

            if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
                ImGui.OpenPopup($"StepContentMenu_{i}");

            using var context = ImRaii.ContextPopupItem($"StepContentMenu_{i}");
            if (!context) return;

            ImGui.Text($"第 {i + 1} 步: {step.Note}");

            ImGui.Separator();

            if (ImGui.MenuItem("复制"))
                StepToCopy = ExecutorPresetStep.Copy(step);

            if (StepToCopy != null)
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

                ImGuiOm.TooltipHover($"已复制步骤: {StepToCopy.Note}");
            }

            if (ImGui.MenuItem("删除"))
                contextOperation = StepOperationType.Delete;

            if (i > 0)
                if (ImGui.MenuItem("上移"))
                    contextOperation = StepOperationType.MoveUp;

            if (i < Steps.Count - 1)
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
                StepOperationType.Delete   => () => Steps.RemoveAt(i),
                StepOperationType.MoveDown => () =>
                {
                    var index = i + 1;
                    Steps.Swap(i, index);
                    CurrentStep = index;
                },
                StepOperationType.MoveUp => () =>
                {
                    var index = i - 1;
                    Steps.Swap(i, index);
                    CurrentStep = index;
                },
                StepOperationType.Pass   => () => { },
                StepOperationType.Paste => () =>
                {
                    Steps[i]    = ExecutorPresetStep.Copy(StepToCopy);
                    CurrentStep = i;
                },
                StepOperationType.PasteUp => () =>
                {
                    Steps.Insert(i, ExecutorPresetStep.Copy(StepToCopy));
                    CurrentStep = i;
                },
                StepOperationType.PasteDown => () =>
                {
                    var index = i + 1;
                    Steps.Insert(index, ExecutorPresetStep.Copy(StepToCopy));
                    CurrentStep = index;
                },
                StepOperationType.InsertUp => () =>
                {
                    Steps.Insert(i, new());
                    CurrentStep = i;
                },
                StepOperationType.InsertDown => () =>
                {
                    var index = i  + 1;
                    Steps.Insert(index, new());
                    CurrentStep = index;
                },
                StepOperationType.PasteCurrent => () =>
                {
                    Steps.Insert(i, ExecutorPresetStep.Copy(step));
                    CurrentStep = i;
                },
                _                        => () => { }
            };
            contextOperationAction();
        }
    }

    public List<Action> GetTasks(TaskHelper t) => 
        Steps.SelectMany(x => x.GetTasks(t)).ToList();

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
            Chat($"已成功导出预设至剪贴板", Main.UTHPrefix);
        }
        catch (Exception)
        {
            Chat($"尝试导出预设至剪贴板时发生错误", Main.UTHPrefix);
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
                    Chat($"已成功从剪贴板导入预设", Main.UTHPrefix);
                return config;
            }
        }
        catch (Exception)
        {
            Chat($"尝试从剪贴板导入预设时发生错误", Main.UTHPrefix);
        }
        return null;
    }

    public override bool Equals(object? obj) => Equals(obj as ExecutorPreset);

    public override int GetHashCode() => HashCode.Combine(Name, Zone, Steps);
}

