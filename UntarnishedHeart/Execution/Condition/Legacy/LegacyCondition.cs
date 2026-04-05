using UntarnishedHeart.Execution.Condition.Enums;

namespace UntarnishedHeart.Execution.Condition.Legacy;

internal sealed class LegacyCondition : Condition
{
    public override ConditionDetectType Kind => DetectType;

    public required ConditionDetectType DetectType { get; init; }

    public required ConditionComparisonType ComparisonType { get; init; }

    public required ConditionTargetType TargetType { get; init; }

    public required float Value { get; init; }

    public override bool Evaluate(in ConditionContext context) =>
        MigrateLegacyV1ToV2(DetectType, ComparisonType, TargetType, Value).Evaluate(context);

    public override Condition DeepCopy() =>
        new LegacyCondition
        {
            DetectType     = DetectType,
            ComparisonType = ComparisonType,
            TargetType     = TargetType,
            Value          = Value
        };

    protected override void DrawBody()
    {
        DrawLabel("迁移状态", KnownColor.Gold.ToVector4());
        ImGui.TextUnformatted("旧版本配置, 请重启一次插件以自动转换为新配置");
    }

    protected override string Describe() =>
        $"旧条件 {DetectType.GetDescription()} / {ComparisonType.GetDescription()} / {TargetType.GetDescription()} / {Value}";
}
