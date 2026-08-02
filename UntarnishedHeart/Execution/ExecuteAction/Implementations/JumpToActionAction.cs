using Newtonsoft.Json;
using UntarnishedHeart.Execution.ExecuteAction.Configuration;
using UntarnishedHeart.Execution.ExecuteAction.Enums;
using UntarnishedHeart.Windows;

namespace UntarnishedHeart.Execution.ExecuteAction.Implementations;

[JsonObject(MemberSerialization.OptIn)]
[ExecuteActionJsonType("JumpToAction", ExecuteActionKind.JumpToAction)]
public sealed class JumpToActionAction : ExecuteActionBase
{
    [JsonProperty("ActionIndex")]
    public int ActionIndex { get; set; }

    public override ExecuteActionKind Kind => ExecuteActionKind.JumpToAction;

    public override void Draw(ExecuteActionDrawContext context)
    {
        var hasValidTarget = ActionIndex >= 0 && ActionIndex < context.Actions.Count;
        var preview        = hasValidTarget ? $"{ActionIndex}. {context.Actions[ActionIndex].Name}" : "请选择目标动作";
        ImGui.SetNextItemWidth(240f * GlobalUIScale);

        using var combo = ImRaii.Combo("目标动作###JumpToActionCombo", preview, ImGuiComboFlags.HeightLargest);
        if (combo)
            ImGui.CloseCurrentPopup();

        if (!ImGui.IsItemClicked())
            return;

        CollectionSelectorWindow.Open
        (
            "选择目标动作",
            "当前步骤暂无动作",
            hasValidTarget ? ActionIndex : -1,
            context.Actions,
            static action => action.Name,
            index =>
            {
                if ((uint)index >= (uint)context.Actions.Count)
                    return;

                ActionIndex = index;
            }
        );
    }

    protected override bool EqualsCore(ExecuteActionBase other) =>
        other is JumpToActionAction action && ActionIndex == action.ActionIndex;

    protected override int GetCoreHashCode() => ActionIndex;

    public override ExecuteActionBase DeepCopy() =>
        CopyBasePropertiesTo(new JumpToActionAction { ActionIndex = ActionIndex });
}
