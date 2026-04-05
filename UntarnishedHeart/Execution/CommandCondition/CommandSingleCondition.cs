using System.Numerics;
using Newtonsoft.Json;
using UntarnishedHeart.Execution.CommandCondition.Enums;
using UntarnishedHeart.Execution.CommandCondition.Legacy;

namespace UntarnishedHeart.Execution.CommandCondition;

[JsonConverter(typeof(CommandSingleConditionJsonConverter))]
public abstract class CommandSingleCondition
{
    protected const float EQUALITY_TOLERANCE = 0.01f;

    public abstract CommandDetectType Kind { get; }

    public CommandSingleCondition Draw(int index)
    {
        using var id    = ImRaii.PushId($"CommandSingleCondition-{index}");
        using var group = ImRaii.Group();
        using var table = ImRaii.Table("SingleConditionTable", 2);
        if (!table)
            return this;

        ImGui.TableSetupColumn("名称", ImGuiTableColumnFlags.WidthFixed, ImGui.CalcTextSize("六个中国汉字").X);
        ImGui.TableSetupColumn("内容", ImGuiTableColumnFlags.WidthStretch);

        var current = DrawKindSelector();
        current.DrawBody();
        return current;
    }

    public abstract bool Evaluate(in CommandConditionContext context);

    public abstract CommandSingleCondition DeepCopy();

    public sealed override string ToString() => Describe();

    protected abstract void DrawBody();

    protected abstract string Describe();

    protected static void DrawLabel(string text, Vector4 color)
    {
        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        ImGui.AlignTextToFramePadding();
        ImGui.TextColored(color, $"{text}:");
        ImGui.TableNextColumn();
        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X - ImGui.GetStyle().ItemSpacing.X);
    }

    protected static TEnum DrawEnumCombo<TEnum>(string id, TEnum current)
        where TEnum : struct, Enum
    {
        using var combo = ImRaii.Combo(id, current.GetDescription(), ImGuiComboFlags.HeightLargest);
        if (!combo)
            return current;

        foreach (var candidate in Enum.GetValues<TEnum>())
        {
            if (ImGui.Selectable(candidate.GetDescription(), EqualityComparer<TEnum>.Default.Equals(current, candidate)))
                current = candidate;

            ImGuiOm.TooltipHover(candidate.GetDescription());
        }

        return current;
    }

    protected static CommandSingleCondition CreateDefault(CommandDetectType kind) =>
        kind switch
        {
            CommandDetectType.Health          => new HealthCommandCondition(),
            CommandDetectType.Status          => new StatusCommandCondition(),
            CommandDetectType.ActionCooldown  => new ActionCooldownCommandCondition(),
            CommandDetectType.ActionCastStart => new ActionCastStartCommandCondition(),
            _                                 => new HealthCommandCondition()
        };

    internal static CommandSingleCondition MigrateLegacyV1ToV2
    (
        CommandDetectType     detectType,
        CommandComparisonType comparisonType,
        CommandTargetType     targetType,
        float                 value
    ) =>
        detectType switch
        {
            CommandDetectType.Health => new HealthCommandCondition
            {
                TargetType = targetType,
                ComparisonType = comparisonType switch
                {
                    CommandComparisonType.GreaterThan => NumericComparisonType.GreaterThan,
                    CommandComparisonType.EqualTo     => NumericComparisonType.EqualTo,
                    CommandComparisonType.NotEqualTo  => NumericComparisonType.NotEqualTo,
                    _                                 => NumericComparisonType.LessThan
                },
                Threshold = value
            },
            CommandDetectType.Status => new StatusCommandCondition
            {
                TargetType     = targetType,
                ComparisonType = comparisonType == CommandComparisonType.NotHas ? PresenceComparisonType.NotHas : PresenceComparisonType.Has,
                StatusID       = (uint)Math.Max(0, value)
            },
            CommandDetectType.ActionCooldown => new ActionCooldownCommandCondition
            {
                ComparisonType = comparisonType == CommandComparisonType.NotFinished ? CooldownComparisonType.NotFinished : CooldownComparisonType.Finished,
                ActionID       = (uint)Math.Max(0, value)
            },
            CommandDetectType.ActionCastStart => new ActionCastStartCommandCondition
            {
                ActionID = (uint)Math.Max(0, value)
            },
            _ => new HealthCommandCondition()
        };

    public static CommandSingleCondition Copy(CommandSingleCondition source) => source.DeepCopy();

    private CommandSingleCondition DrawKindSelector()
    {
        DrawLabel("检测类型", KnownColor.LightSkyBlue.ToVector4());

        var selectedKind = DrawEnumCombo("###DetectTypeCombo", Kind);
        if (selectedKind == Kind)
            return this;

        return CreateDefault(selectedKind);
    }

    protected static IBattleChara? ResolveTarget(in CommandConditionContext context, CommandTargetType targetType) =>
        targetType switch
        {
            CommandTargetType.Self   => context.LocalPlayer,
            CommandTargetType.Target => context.Target,
            _                        => null
        };
}
