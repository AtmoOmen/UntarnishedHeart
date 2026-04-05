using System.Numerics;
using Newtonsoft.Json;
using UntarnishedHeart.Execution.Condition.Enums;
using UntarnishedHeart.Execution.Condition.Legacy;

namespace UntarnishedHeart.Execution.Condition;

[JsonConverter(typeof(ConditionJsonConverter))]
public abstract class Condition
{
    protected const float EQUALITY_TOLERANCE = 0.01f;

    public abstract ConditionDetectType Kind { get; }

    public Condition Draw(int index)
    {
        using var id    = ImRaii.PushId($"Condition-{index}");
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

    public abstract bool Evaluate(in ConditionContext context);

    public abstract Condition DeepCopy();

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

    protected static Condition CreateDefault(ConditionDetectType kind) =>
        kind switch
        {
            ConditionDetectType.Health          => new HealthCondition(),
            ConditionDetectType.Status          => new StatusCondition(),
            ConditionDetectType.ActionCooldown  => new ActionCooldownCondition(),
            ConditionDetectType.ActionCastStart => new ActionCastStartCondition(),
            _                                 => new HealthCondition()
        };

    internal static Condition MigrateLegacyV1ToV2
    (
        ConditionDetectType     detectType,
        ConditionComparisonType comparisonType,
        ConditionTargetType     targetType,
        float                 value
    ) =>
        detectType switch
        {
            ConditionDetectType.Health => new HealthCondition
            {
                TargetType = targetType,
                ComparisonType = comparisonType switch
                {
                    ConditionComparisonType.GreaterThan => NumericComparisonType.GreaterThan,
                    ConditionComparisonType.EqualTo     => NumericComparisonType.EqualTo,
                    ConditionComparisonType.NotEqualTo  => NumericComparisonType.NotEqualTo,
                    _                                 => NumericComparisonType.LessThan
                },
                Threshold = value
            },
            ConditionDetectType.Status => new StatusCondition
            {
                TargetType     = targetType,
                ComparisonType = comparisonType == ConditionComparisonType.NotHas ? PresenceComparisonType.NotHas : PresenceComparisonType.Has,
                StatusID       = (uint)Math.Max(0, value)
            },
            ConditionDetectType.ActionCooldown => new ActionCooldownCondition
            {
                ComparisonType = comparisonType == ConditionComparisonType.NotFinished ? CooldownComparisonType.NotFinished : CooldownComparisonType.Finished,
                ActionID       = (uint)Math.Max(0, value)
            },
            ConditionDetectType.ActionCastStart => new ActionCastStartCondition
            {
                ActionID = (uint)Math.Max(0, value)
            },
            _ => new HealthCondition()
        };

    public static Condition Copy(Condition source) => source.DeepCopy();

    private Condition DrawKindSelector()
    {
        DrawLabel("检测类型", KnownColor.LightSkyBlue.ToVector4());

        var selectedKind = DrawEnumCombo("###DetectTypeCombo", Kind);
        if (selectedKind == Kind)
            return this;

        return CreateDefault(selectedKind);
    }

    protected static IBattleChara? ResolveTarget(in ConditionContext context, ConditionTargetType targetType) =>
        targetType switch
        {
            ConditionTargetType.Self   => context.LocalPlayer,
            ConditionTargetType.Target => context.Target,
            _                        => null
        };
}
