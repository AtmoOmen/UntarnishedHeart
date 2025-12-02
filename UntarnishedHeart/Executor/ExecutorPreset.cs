using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Utility;
using Dalamud.Interface;
using Dalamud.Bindings.ImGui;
using System.Collections.Generic;
using System;
using System.Linq;
using System.Numerics;
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
        using var table = ImRaii.Table("PresetBasicInfoTable", 2, ImGuiTableFlags.Resizable);
        if (!table) return;

        ImGui.TableSetupColumn("Label", ImGuiTableColumnFlags.WidthFixed);
        ImGui.TableSetupColumn("Control", ImGuiTableColumnFlags.WidthStretch);

        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        
        ImGui.AlignTextToFramePadding();
        ImGui.Text("名称:");
        
        ImGui.TableNextColumn();
        ImGui.SetNextItemWidth(-1f);
        var name = Name;
        if (ImGui.InputText("###PresetNameInput", ref name, 128))
            Name = name;

        ImGui.TableNextRow();
        ImGui.TableSetColumnIndex(0);
        ImGui.AlignTextToFramePadding();
        ImGui.Text("副本区域:");
        ImGui.TableSetColumnIndex(1);
        var zone = (uint)Zone;
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

        ImGui.TableNextRow();
        ImGui.TableSetColumnIndex(0);
        ImGui.AlignTextToFramePadding();
        ImGui.Text("退出延迟:");
        ImGui.TableSetColumnIndex(1);
        var delay = DutyDelay;
        ImGui.SetNextItemWidth(350f * ImGuiHelpers.GlobalScale);
        if (ImGui.InputInt("(ms)###PresetLeaveDutyDelayInput", ref delay))
            DutyDelay = Math.Max(0, delay);
        ImGuiOm.TooltipHover("完成副本后, 在退出副本前需要等待的时间");

        ImGui.TableNextRow();
        ImGui.TableSetColumnIndex(0);
        ImGui.AlignTextToFramePadding();
        ImGui.Text("自动开启:");
        ImGui.TableSetColumnIndex(1);
        var autoOpenTreasure = AutoOpenTreasures;
        if (ImGui.Checkbox("副本结束时, 自动开启宝箱", ref autoOpenTreasure))
            AutoOpenTreasures = autoOpenTreasure;
        ImGuiOm.HelpMarker("请确保本副本的确有宝箱, 否则流程将卡死", 20f, FontAwesomeIcon.InfoCircle, true);

        ImGui.TableNextRow();
        ImGui.TableSetColumnIndex(0);
        ImGui.AlignTextToFramePadding();
        ImGui.Text("备注:");
        ImGui.TableSetColumnIndex(1);
        var remark = Remark;
        if (ImGui.InputTextMultiline("###RemarkInput", ref remark, 2056, new(-1f)))
            Remark = remark;
    }

    private unsafe void DrawStepInfo()
    {
        if (Steps.Count == 0)
            CurrentStep = -1;
        else if (CurrentStep >= Steps.Count)
            CurrentStep = Steps.Count - 1;

        using var table = ImRaii.Table("PresetStepsTable", 2, ImGuiTableFlags.Resizable | ImGuiTableFlags.BordersInnerV);
        if (!table) return;

        ImGui.TableSetupColumn("StepsList", ImGuiTableColumnFlags.WidthFixed, 300f * ImGuiHelpers.GlobalScale);
        ImGui.TableSetupColumn("StepDetails", ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableNextRow();

        ImGui.TableSetColumnIndex(0);
        if (ImGuiOm.ButtonSelectable("添加步骤"))
            Steps.Add(new());
        ImGuiOm.TooltipHover("每一步骤内的各判断, 都遵循界面绘制顺序, 从上到下、从左到右依次判断执行");

        using (var child = ImRaii.Child("StepsSelectChild", ImGui.GetContentRegionAvail(), true))
        {
            if (child)
            {
                for (var i = 0; i < Steps.Count; i++)
                {
                    var step = Steps[i];
                    var stepName = $"{i}. {step.Note}" + (step.Delay > 0 ? $" ({(float)step.Delay / 1000:F2}s)" : string.Empty);

                    if (ImGui.Selectable(stepName, i == CurrentStep, ImGuiSelectableFlags.AllowDoubleClick))
                        CurrentStep = i;

                    if (ImGui.BeginDragDropSource(ImGuiDragDropFlags.None))
                    {
                        ImGui.SetDragDropPayload("STEP_REORDER", BitConverter.GetBytes(i));
                        ImGui.Text($"步骤: {stepName}");
                        ImGui.EndDragDropSource();
                    }

                    if (ImGui.BeginDragDropTarget())
                    {
                        var payload = ImGui.AcceptDragDropPayload("STEP_REORDER");
                        if (!payload.IsNull && payload.Data != null)
                        {
                            var sourceIndex = *(int*)payload.Data;
                            if (sourceIndex != i && sourceIndex >= 0 && sourceIndex < Steps.Count)
                            {
                                (Steps[sourceIndex], Steps[i]) = (Steps[i], Steps[sourceIndex]);

                                if (CurrentStep == sourceIndex)
                                    CurrentStep = i;
                                else if (CurrentStep == i)
                                    CurrentStep = sourceIndex;
                            }
                        }

                        ImGui.EndDragDropTarget();
                    }

                    DrawStepContextMenu(i, step);
                }
            }
        }

        ImGui.TableSetColumnIndex(1);
        using (var child = ImRaii.Child("StepsDrawChild", ImGui.GetContentRegionAvail(), true, ImGuiWindowFlags.NoBackground))
        {
            if (child)
            {
                if (CurrentStep < 0 || CurrentStep >= Steps.Count)
                {
                    ImGui.TextColored(new Vector4(0.7f, 0.7f, 0.7f, 1f), "请选择一个步骤进行编辑");
                    return;
                }

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

            ImGui.Text($"第 {i} 步: {step.Note}");

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

    public List<Action> GetTasks(TaskHelper t)
    {
        var tasks = new List<Action>();
        foreach (var step in Steps)
        {
            tasks.AddRange(step.GetTasks(t));

            var step1 = step;
            tasks.Add(() =>
            {
                var target = step1.JumpToIndex;
                if (target < 0 || target >= Steps.Count) return;

                t.Enqueue(() =>
                          {
                              t.RemoveAllTasks(0);
                              for (var j = target; j < Steps.Count; j++)
                              {
                                  foreach (var action in Steps[j].GetTasks(t))
                                      action.Invoke();
                              }
                              return true;
                          }, $"跳转至第 {target} 步");
            });
        }

        return tasks;
    }

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

