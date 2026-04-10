using System.Runtime.CompilerServices;
using Lumina.Excel.Sheets;
using OmenTools.ImGuiOm.Widgets.Combos;
using OmenTools.Interop.Game.Lumina;
using UntarnishedHeart.Execution.Common;
using UntarnishedHeart.Execution.Enums;
using UntarnishedHeart.Execution.Managers;
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
                DrawBasicInfoTab(preset, state);
        }

        using (var stepInfo = ImRaii.TabItem("步骤"))
        {
            if (stepInfo)
                DrawStepsTab(preset, state);
        }
        
        
        if (!string.IsNullOrEmpty(state.TreeState.CurrentPathTabLabel))
        {
            ImGui.TabItemButton("###Space");
            
            using (ImRaii.Disabled())
                ImGui.TabItemButton($"{state.TreeState.CurrentPathTabLabel}###PathLabel");
        }
    }

    private static void DrawBasicInfoTab(Preset preset, PresetEditorState state)
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

    private static void DrawStepsTab(Preset preset, PresetEditorState state)
    {
        StepTreeEditor.Draw
        (
            "Preset",
            preset.Steps,
            state.TreeState,
            state.SharedState,
            GetRunningCursor(preset),
            () => new PresetStep { Name = $"步骤 {preset.Steps.Count}" }
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

    private static ExecuteActionRuntimeCursor? GetRunningCursor(Preset preset)
    {
        if (ExecutionManager.PresetExecutor is not { IsDisposed: false, Completion.IsCompleted: false, ExecutorPreset: not null } presetExecutor)
            return null;

        return ReferenceEquals(presetExecutor.ExecutorPreset, preset) ? presetExecutor.Progress.RuntimeCursor : null;
    }

    internal sealed class PresetEditorState
    (
        Preset preset
    )
    {
        public StepTreeEditorState   TreeState    { get; }      = new();
        public StepEditorSharedState SharedState  { get; }      = new();
        public ContentSelectCombo    ContentCombo { get; }      = new(preset.ToString())
        {
            SelectedID = GetContentFinderConditionID(preset.Zone)
        };
    }
}
