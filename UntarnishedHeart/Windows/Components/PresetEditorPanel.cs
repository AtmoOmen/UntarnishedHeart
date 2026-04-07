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
        ImGui.TextColored(KnownColor.LightSkyBlue.ToUInt(), "名称");

        var name = preset.Name;
        ImGui.SetNextItemWidth(-1f);
        if (ImGui.InputText("###PresetNameInput", ref name, 128))
            preset.Name = name;

        ImGui.TextColored(KnownColor.LightSkyBlue.ToUInt(), "副本区域");

        ImGui.SetNextItemWidth(-1f);
        if (state.ContentCombo.DrawRadio())
            preset.Zone = (ushort)state.ContentCombo.SelectedItem.TerritoryType.RowId;

        ImGui.TextColored(KnownColor.LightSkyBlue.ToUInt(), "退出延迟 (毫秒)");
        ImGuiOm.HelpMarker("副本完成时, 等待多长时间后自动离开副本");

        var dutyDelay = preset.DutyDelay;
        ImGui.SetNextItemWidth(-1f);
        if (ImGui.InputInt("(ms)###PresetLeaveDutyDelayInput", ref dutyDelay))
            preset.DutyDelay = Math.Max(0, dutyDelay);

        var autoOpenTreasure = preset.AutoOpenTreasures;
        ImGui.SetNextItemWidth(-1f);
        if (ImGui.Checkbox("自动开启宝箱", ref autoOpenTreasure))
            preset.AutoOpenTreasures = autoOpenTreasure;
        ImGuiOm.HelpMarker("副本进行时, 会自动记录所有宝箱位置, 并在副本结束时, 挨个瞬移至记录位置并尝试开启宝箱");

        ImGui.TextColored(KnownColor.LightSkyBlue.ToUInt(), "备注");

        var remark = preset.Remark;
        if (ImGui.InputTextMultiline("###RemarkInput", ref remark, 4096, new(-1f)))
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
        if (ImGuiOm.ButtonStretch("添加步骤"))
            preset.Steps.Add(new PresetStep { Name = $"步骤 {preset.Steps.Count}" });

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

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
            ImGui.TextDisabled("请选择一个步骤进行编辑");
            return;
        }

        var currentStep      = preset.Steps[state.CurrentStep];
        var currentStepIndex = state.CurrentStep;
        StepEditor.Draw(currentStep, ref currentStepIndex, preset.Steps, state.SharedState);
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
            state.SharedState.StepToCopy = PresetStep.Copy(step);

        if (state.SharedState.StepToCopy != null)
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
            state.SharedState.StepToCopy == null ? null : () => PresetStep.Copy(state.SharedState.StepToCopy),
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

    internal sealed class PresetEditorState
    {
        public int                   CurrentStep  { get; set; } = -1;
        public StepEditorSharedState SharedState  { get; }      = new();
        public ContentSelectCombo    ContentCombo { get; }

        public PresetEditorState(Preset preset)
        {
            ContentCombo = new(preset.ToString())
            {
                SelectedID = GetContentFinderConditionID(preset.Zone)
            };
        }
    }
}
