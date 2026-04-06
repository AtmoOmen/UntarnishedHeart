using System.Numerics;
using System.Runtime.CompilerServices;
using Lumina.Excel.Sheets;
using OmenTools.ImGuiOm.Widgets.Combos;
using OmenTools.Interop.Game.Lumina;
using UntarnishedHeart.Execution.Enums;
using UntarnishedHeart.Execution.Preset;
using UntarnishedHeart.Windows.Helpers;

namespace UntarnishedHeart.Windows.Components;

internal static class PresetEditorPanel
{
    private static readonly ConditionalWeakTable<Preset, PresetEditorState> EditorStates = [];

    public static void Draw(Preset preset)
    {
        var state = EditorStates.GetValue(preset, static value => new PresetEditorState(value));
        SyncContentCombo(state, preset);

        using var tabBar = ImRaii.TabBar("###ExecutorPresetEditor");
        if (!tabBar) return;

        using (var basicInfo = ImRaii.TabItem("基本信息"))
        {
            if (basicInfo)
                DrawBasicInfo(preset, state);
        }

        using (var stepInfo = ImRaii.TabItem("步骤"))
        {
            if (stepInfo)
                DrawStepInfo(preset, state);
        }
    }

    private static void DrawBasicInfo(Preset preset, PresetEditorState state)
    {
        using var table = ImRaii.Table("PresetBasicInfoTable", 2, ImGuiTableFlags.Resizable);
        if (!table) return;

        ImGui.TableSetupColumn("Label",   ImGuiTableColumnFlags.WidthFixed);
        ImGui.TableSetupColumn("Control", ImGuiTableColumnFlags.WidthStretch);

        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        ImGui.AlignTextToFramePadding();
        ImGui.Text("名称:");
        ImGui.TableNextColumn();
        ImGui.SetNextItemWidth(-1f);
        var name = preset.Name;
        if (ImGui.InputText("###PresetNameInput", ref name, 128))
            preset.Name = name;

        ImGui.TableNextRow();
        ImGui.TableSetColumnIndex(0);
        ImGui.AlignTextToFramePadding();
        ImGui.Text("副本区域:");
        ImGui.TableSetColumnIndex(1);
        ImGui.SetNextItemWidth(350f * GlobalUIScale);

        if (state.ContentCombo.DrawRadio())
            preset.Zone = (ushort)state.ContentCombo.SelectedItem.TerritoryType.RowId;

        ImGui.SameLine();

        if (ImGuiOm.ButtonIcon("GetZone", FontAwesomeIcon.MapMarkedAlt, "取当前区域", true))
            preset.Zone = DService.Instance().ClientState.TerritoryType;

        using (ImRaii.PushIndent())
        {
            if (LuminaGetter.TryGetRow<TerritoryType>(preset.Zone, out var zoneData))
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
        var dutyDelay = preset.DutyDelay;
        ImGui.SetNextItemWidth(350f * GlobalUIScale);
        if (ImGui.InputInt("(ms)###PresetLeaveDutyDelayInput", ref dutyDelay))
            preset.DutyDelay = Math.Max(0, dutyDelay);

        ImGui.TableNextRow();
        ImGui.TableSetColumnIndex(0);
        ImGui.AlignTextToFramePadding();
        ImGui.Text("自动开启:");
        ImGui.TableSetColumnIndex(1);
        var autoOpenTreasure = preset.AutoOpenTreasures;
        if (ImGui.Checkbox("副本结束时, 自动开启宝箱", ref autoOpenTreasure))
            preset.AutoOpenTreasures = autoOpenTreasure;

        ImGui.TableNextRow();
        ImGui.TableSetColumnIndex(0);
        ImGui.AlignTextToFramePadding();
        ImGui.Text("备注:");
        ImGui.TableSetColumnIndex(1);
        var remark = preset.Remark;
        if (ImGui.InputTextMultiline("###RemarkInput", ref remark, 4096, new(-1f, 120f * GlobalUIScale)))
            preset.Remark = remark;
    }

    private static unsafe void DrawStepInfo(Preset preset, PresetEditorState state)
    {
        state.CurrentStep = NormalizeCurrentStep(state.CurrentStep, preset.Steps.Count);

        using var table = ImRaii.Table("PresetStepsTable", 2, ImGuiTableFlags.Resizable | ImGuiTableFlags.BordersInnerV);
        if (!table) return;

        ImGui.TableSetupColumn("StepsList",   ImGuiTableColumnFlags.WidthFixed, 200f * GlobalUIScale);
        ImGui.TableSetupColumn("StepDetails", ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableNextRow();

        ImGui.TableSetColumnIndex(0);
        if (ImGuiOm.ButtonSelectable("添加步骤"))
            preset.Steps.Add(new PresetStep { Name = $"步骤 {preset.Steps.Count}" });

        using (var child = ImRaii.Child("StepsSelectChild", ImGui.GetContentRegionAvail(), true))
        {
            if (child)
            {
                for (var i = 0; i < preset.Steps.Count; i++)
                {
                    var step        = preset.Steps[i];
                    var actionCount = step.EnterActions.Count + step.BodyActions.Count + step.ExitActions.Count;
                    var stepName    = $"{i}. {step.Name} ({actionCount} 个动作)";

                    if (ImGui.Selectable(stepName, i == state.CurrentStep, ImGuiSelectableFlags.AllowDoubleClick))
                        state.CurrentStep = i;

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

                            if (sourceIndex != i && sourceIndex >= 0 && sourceIndex < preset.Steps.Count)
                            {
                                (preset.Steps[sourceIndex], preset.Steps[i]) = (preset.Steps[i], preset.Steps[sourceIndex]);

                                if (state.CurrentStep == sourceIndex)
                                    state.CurrentStep = i;
                                else if (state.CurrentStep == i)
                                    state.CurrentStep = sourceIndex;
                            }
                        }

                        ImGui.EndDragDropTarget();
                    }

                    DrawStepContextMenu(preset, state, i, step);
                }
            }
        }

        ImGui.TableSetColumnIndex(1);

        using var detailsChild = ImRaii.Child("StepsDrawChild", ImGui.GetContentRegionAvail(), true, ImGuiWindowFlags.NoBackground);
        if (!detailsChild) return;

        if (state.CurrentStep < 0 || state.CurrentStep >= preset.Steps.Count)
        {
            ImGui.TextColored(new Vector4(0.7f, 0.7f, 0.7f, 1f), "请选择一个步骤进行编辑");
            return;
        }

        var currentStep      = preset.Steps[state.CurrentStep];
        var currentStepIndex = state.CurrentStep;
        PresetStepEditor.Draw(currentStep, ref currentStepIndex, preset.Steps);
        state.CurrentStep = currentStepIndex;
    }

    private static void DrawStepContextMenu(Preset preset, PresetEditorState state, int index, PresetStep step)
    {
        var contextOperation = StepOperationType.Pass;

        if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
            ImGui.OpenPopup($"StepContentMenu_{index}");

        using var context = ImRaii.ContextPopupItem($"StepContentMenu_{index}");
        if (!context) return;

        ImGui.Text($"第 {index} 步: {step.Name}");
        ImGui.Separator();

        if (ImGui.MenuItem("复制"))
            state.StepToCopy = PresetStep.Copy(step);

        if (state.StepToCopy != null)
        {
            if (ImGui.MenuItem("粘贴至本步"))
                contextOperation = StepOperationType.Paste;

            if (ImGui.MenuItem("向上插入粘贴"))
                contextOperation = StepOperationType.PasteUp;

            if (ImGui.MenuItem("向下插入粘贴"))
                contextOperation = StepOperationType.PasteDown;
        }

        if (ImGui.MenuItem("删除"))
            contextOperation = StepOperationType.Delete;

        if (index > 0 && ImGui.MenuItem("上移"))
            contextOperation = StepOperationType.MoveUp;

        if (index < preset.Steps.Count - 1 && ImGui.MenuItem("下移"))
            contextOperation = StepOperationType.MoveDown;

        ImGui.Separator();

        if (ImGui.MenuItem("向上插入新步骤"))
            contextOperation = StepOperationType.InsertUp;

        if (ImGui.MenuItem("向下插入新步骤"))
            contextOperation = StepOperationType.InsertDown;

        ImGui.Separator();

        if (ImGui.MenuItem("复制并插入本步骤"))
            contextOperation = StepOperationType.PasteCurrent;

        state.CurrentStep = CollectionOperationHelper.Apply
        (
            preset.Steps,
            index,
            contextOperation,
            state.CurrentStep,
            () => new PresetStep { Name = $"步骤 {preset.Steps.Count}" },
            state.StepToCopy == null ? null : () => PresetStep.Copy(state.StepToCopy),
            () => PresetStep.Copy(step)
        );
    }

    private static void SyncContentCombo(PresetEditorState state, Preset preset)
    {
        var selectedContentID = GetContentFinderConditionID(preset.Zone);
        if (state.ContentCombo.SelectedID != selectedContentID)
            state.ContentCombo.SelectedID = selectedContentID;
    }

    private static uint GetContentFinderConditionID(ushort zone) =>
        LuminaGetter.GetRow<TerritoryType>(zone)?.ContentFinderCondition.RowId ?? 0;

    private static int NormalizeCurrentStep(int currentStep, int count)
    {
        if (count == 0)
            return -1;

        return Math.Clamp(currentStep, 0, count - 1);
    }

    private sealed class PresetEditorState
    {
        public int                CurrentStep  { get; set; } = -1;
        public PresetStep?        StepToCopy   { get; set; }
        public ContentSelectCombo ContentCombo { get; }

        public PresetEditorState(Preset preset)
        {
            ContentCombo = new(preset.ToString())
            {
                SelectedID = GetContentFinderConditionID(preset.Zone)
            };
        }
    }
}
