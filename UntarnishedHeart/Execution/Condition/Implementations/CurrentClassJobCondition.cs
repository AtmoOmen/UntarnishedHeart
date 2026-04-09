using Newtonsoft.Json;
using OmenTools.ImGuiOm.Widgets.Combos;
using OmenTools.OmenService;
using UntarnishedHeart.Execution.Condition.Configuration;
using UntarnishedHeart.Execution.Condition.Enums;

namespace UntarnishedHeart.Execution.Condition;

[JsonObject(MemberSerialization.OptIn)]
[ConditionJsonType("CurrentClassJob", ConditionDetectType.CurrentClassJob)]
public sealed class CurrentClassJobCondition : ConditionBase
{
    private readonly string jobComboID = $"CurrentClassJob_{Guid.NewGuid():N}";

    [JsonProperty("JobId")]
    public uint JobID { get; set; }

    public override ConditionDetectType Kind => ConditionDetectType.CurrentClassJob;

    public override bool Evaluate(in ConditionContext context) =>
        JobID != 0 && LocalPlayerState.ClassJob == JobID;

    protected override bool EqualsCore(ConditionBase other) =>
        other is CurrentClassJobCondition condition &&
        JobID == condition.JobID;

    protected override int GetCoreHashCode() => (int)JobID;

    public override ConditionBase DeepCopy() =>
        CopyBasePropertiesTo(new CurrentClassJobCondition { JobID = JobID });

    protected override void DrawBody()
    {
        DrawLabel("职业", KnownColor.LightSkyBlue.ToVector4());
        var combo = new JobSelectCombo(jobComboID) { SelectedID = JobID };
        if (combo.DrawRadio())
            JobID = combo.SelectedID;
    }
}
