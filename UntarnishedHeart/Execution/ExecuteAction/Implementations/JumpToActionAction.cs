using Newtonsoft.Json;
using UntarnishedHeart.Execution.ExecuteAction.Configuration;
using UntarnishedHeart.Execution.ExecuteAction.Enums;

namespace UntarnishedHeart.Execution.ExecuteAction.Implementations;

[JsonObject(MemberSerialization.OptIn)]
[ExecuteActionJsonType("JumpToAction", ExecuteActionKind.JumpToAction)]
public sealed class JumpToActionAction : ExecuteActionBase
{
    [JsonProperty("ActionIndex")]
    public int ActionIndex { get; set; }

    public override ExecuteActionKind Kind => ExecuteActionKind.JumpToAction;

    public override void Draw()
    {
        var actionIndex = ActionIndex;
        ImGui.SetNextItemWidth(240f * GlobalUIScale);
        if (ImGui.InputInt("目标动作索引###JumpToActionInput", ref actionIndex))
            ActionIndex = actionIndex;
    }

    protected override bool EqualsCore(ExecuteActionBase other) =>
        other is JumpToActionAction action && ActionIndex == action.ActionIndex;

    protected override int GetCoreHashCode() => ActionIndex;

    public override ExecuteActionBase DeepCopy() =>
        CopyBasePropertiesTo(new JumpToActionAction { ActionIndex = ActionIndex });
}
