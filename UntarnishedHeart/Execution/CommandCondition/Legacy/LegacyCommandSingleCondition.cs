using UntarnishedHeart.Execution.CommandCondition.Enums;

namespace UntarnishedHeart.Execution.CommandCondition.Legacy;

internal sealed class LegacyCommandSingleCondition : CommandSingleCondition
{
    public override CommandDetectType Kind => DetectType;

    public required CommandDetectType DetectType { get; init; }

    public required CommandComparisonType ComparisonType { get; init; }

    public required CommandTargetType TargetType { get; init; }

    public required float Value { get; init; }

    public override bool Evaluate(in CommandConditionContext context) =>
        MigrateLegacyV1ToV2(DetectType, ComparisonType, TargetType, Value).Evaluate(context);

    public override CommandSingleCondition DeepCopy() =>
        new LegacyCommandSingleCondition
        {
            DetectType     = DetectType,
            ComparisonType = ComparisonType,
            TargetType     = TargetType,
            Value          = Value
        };

    protected override void DrawBody()
    {
        DrawLabel("迁移状态", KnownColor.Gold.ToVector4());
        ImGui.TextUnformatted("旧条件会在下次保存时自动写回新结构");
    }

    protected override string Describe() =>
        $"旧条件 {DetectType.GetDescription()} / {ComparisonType.GetDescription()} / {TargetType.GetDescription()} / {Value}";
}
