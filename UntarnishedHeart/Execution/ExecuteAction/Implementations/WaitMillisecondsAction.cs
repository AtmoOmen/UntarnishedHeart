using Newtonsoft.Json;
using UntarnishedHeart.Execution.ExecuteAction.Configuration;
using UntarnishedHeart.Execution.ExecuteAction.Enums;

namespace UntarnishedHeart.Execution.ExecuteAction.Implementations;

[JsonObject(MemberSerialization.OptIn)]
[ExecuteActionJsonType("Wait", ExecuteActionKind.Wait)]
public sealed class WaitMillisecondsAction : ExecuteActionBase
{
    [JsonProperty("Milliseconds")]
    public int Milliseconds { get; set; }

    public override ExecuteActionKind Kind => ExecuteActionKind.Wait;

    public override void Draw()
    {
        var milliseconds = Milliseconds;
        ImGui.SetNextItemWidth(240f * GlobalUIScale);
        if (ImGui.InputInt("等待时间 (毫秒)###WaitMillisecondsInput", ref milliseconds))
            Milliseconds = Math.Max(0, milliseconds);
    }

    protected override bool EqualsCore(ExecuteActionBase other) => other is WaitMillisecondsAction action && Milliseconds == action.Milliseconds;

    protected override int GetCoreHashCode() => Milliseconds;

    public override ExecuteActionBase DeepCopy() =>
        CopyBasePropertiesTo(new WaitMillisecondsAction { Milliseconds = Milliseconds });
}
