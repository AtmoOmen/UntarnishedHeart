using System.Numerics;
using Newtonsoft.Json;
using UntarnishedHeart.Execution.Condition.Configuration;
using UntarnishedHeart.Execution.Condition.Configuration.Migrators;
using UntarnishedHeart.Execution.Condition.Enums;
using UntarnishedHeart.Execution.Condition.Legacy;
using UntarnishedHeart.Execution.Models;
using UntarnishedHeart.Execution.Preset.Helpers;

namespace UntarnishedHeart.Execution.Condition;

[JsonConverter(typeof(ConditionJSONConverter))]
public abstract class ConditionBase : IEquatable<ConditionBase>
{
    protected const float EQUALITY_TOLERANCE = 0.01f;

    public string Name { get; set; } = string.Empty;

    public string Remark { get; set; } = string.Empty;

    public abstract ConditionDetectType Kind { get; }

    public abstract bool Evaluate(in ConditionContext context);

    public abstract ConditionBase DeepCopy();

    public string GetDefaultName() => Kind.GetDescription();

    public void ResetMetadata()
    {
        Name   = GetDefaultName();
        Remark = string.Empty;
    }

    public bool Equals(ConditionBase? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;

        return Kind == other.Kind      &&
               Name == other.Name     &&
               Remark == other.Remark &&
               EqualsCore(other);
    }

    protected abstract bool EqualsCore(ConditionBase other);

    public override bool Equals(object? obj) => Equals(obj as ConditionBase);

    public override int GetHashCode() => HashCode.Combine((int)Kind, Name, Remark, GetCoreHashCode());

    protected abstract int GetCoreHashCode();

    protected T CopyBasePropertiesTo<T>(T target)
        where T : ConditionBase
    {
        target.Name   = Name;
        target.Remark = Remark;
        return target;
    }

    public ConditionBase Draw(int index)
    {
        using var id    = ImRaii.PushId($"Condition-{index}");
        using var group = ImRaii.Group();
        using var table = ImRaii.Table("SingleConditionTable", 2);
        if (!table)
            return this;

        ImGui.TableSetupColumn("名称", ImGuiTableColumnFlags.WidthFixed, ImGui.CalcTextSize("一二三四五六七八").X);
        ImGui.TableSetupColumn("内容", ImGuiTableColumnFlags.WidthStretch);

        DrawMetadataFields();

        var current = DrawKindSelector();
        current.DrawBody();
        return current;
    }

    protected abstract void DrawBody();

    internal static void DrawLabel(string text, Vector4 color)
    {
        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        ImGui.AlignTextToFramePadding();
        ImGui.TextColored(color, $"{text}:");
        ImGui.TableNextColumn();
        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X - ImGui.GetStyle().ItemSpacing.X);
    }

    internal static TEnum DrawEnumCombo<TEnum>(string id, TEnum current)
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

    protected static ConditionTargetType DrawTargetType(string id, ConditionTargetType current)
    {
        DrawLabel("目标类型", KnownColor.LightSkyBlue.ToVector4());
        return DrawEnumCombo(id, current);
    }

    protected static IBattleChara? ResolveTarget(in ConditionContext context, ConditionTargetType targetType) =>
        targetType switch
        {
            ConditionTargetType.Self   => context.LocalPlayer,
            ConditionTargetType.Target => context.Target,
            _                          => null
        };

    protected static bool CompareNumeric(NumericComparisonType comparisonType, float actualValue, float expectedValue) =>
        comparisonType switch
        {
            NumericComparisonType.GreaterThan        => actualValue                            > expectedValue,
            NumericComparisonType.GreaterThanOrEqual => actualValue                            >= expectedValue,
            NumericComparisonType.LessThan           => actualValue                            < expectedValue,
            NumericComparisonType.LessThanOrEqual    => actualValue                            <= expectedValue,
            NumericComparisonType.EqualTo            => MathF.Abs(actualValue - expectedValue) <= EQUALITY_TOLERANCE,
            NumericComparisonType.NotEqualTo         => MathF.Abs(actualValue - expectedValue) > EQUALITY_TOLERANCE,
            _                                        => false
        };

    protected static bool CompareNumeric(NumericComparisonType comparisonType, int actualValue, int expectedValue) =>
        comparisonType switch
        {
            NumericComparisonType.GreaterThan        => actualValue > expectedValue,
            NumericComparisonType.GreaterThanOrEqual => actualValue >= expectedValue,
            NumericComparisonType.LessThan           => actualValue < expectedValue,
            NumericComparisonType.LessThanOrEqual    => actualValue <= expectedValue,
            NumericComparisonType.EqualTo            => actualValue == expectedValue,
            NumericComparisonType.NotEqualTo         => actualValue != expectedValue,
            _                                        => false
        };

    protected static IGameObject? ResolveSpecificTarget(TargetSelector selector) =>
        PresetTargetResolver.Resolve(selector);

    protected static ConditionBase CreateDefault(ConditionDetectType kind) =>
        InitializeMetadata
        (
            kind switch
            {
                ConditionDetectType.GameCondition              => new GameConditionStateCondition(),
                ConditionDetectType.Status                     => new StatusCondition(),
                ConditionDetectType.Health                     => new HealthCondition(),
                ConditionDetectType.ActionCast                 => new ActionCastCondition(),
                ConditionDetectType.ActionCooldown             => new ActionCooldownCondition(),
                ConditionDetectType.ActionUsable               => new ActionUsableCondition(),
                ConditionDetectType.PositionRange              => new PositionRangeCondition(),
                ConditionDetectType.NearbyTarget               => new NearbyTargetCondition(),
                ConditionDetectType.HasTarget                  => new HasTargetCondition(),
                ConditionDetectType.HasSpecificTarget          => new HasSpecificTargetCondition(),
                ConditionDetectType.PartyAllDead               => new PartyAllDeadCondition(),
                ConditionDetectType.TargetTargetIsSelf         => new TargetTargetIsSelfCondition(),
                ConditionDetectType.PlayerLevel                => new PlayerLevelCondition(),
                ConditionDetectType.OptimalPartyRecommendation => new OptimalPartyRecommendationCondition(),
                ConditionDetectType.CompletedDutyCount         => new CompletedDutyCountCondition(),
                ConditionDetectType.AchievementCount           => new AchievementCountCondition(),
                ConditionDetectType.ItemCount                  => new ItemCountCondition(),
                _                                              => new HealthCondition()
            }
        );

    internal static ConditionBase MigrateLegacyV1ToV2
    (
        ConditionDetectType     detectType,
        ConditionComparisonType comparisonType,
        ConditionTargetType     targetType,
        float                   value
    ) =>
        InitializeMetadata
        (
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
                        _                                   => NumericComparisonType.LessThan
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
                    Action         = new ActionReference { ActionID = (uint)Math.Max(0, value) }
                },
                ConditionDetectType.ActionCast or ConditionDetectType.ActionCastStart => new ActionCastCondition
                {
                    TargetType     = targetType,
                    ComparisonType = comparisonType == ConditionComparisonType.NotHas ? PresenceComparisonType.NotHas : PresenceComparisonType.Has,
                    Action         = new ActionReference { ActionID = (uint)Math.Max(0, value) }
                },
                _ => new HealthCondition()
            }
        );

    private ConditionBase DrawKindSelector()
    {
        DrawLabel("条件类型", KnownColor.LightSkyBlue.ToVector4());

        var selectedKind = DrawEnumCombo("###ConditionKindCombo", Kind);
        if (selectedKind == Kind)
            return this;

        var keepCustomName = !string.IsNullOrEmpty(Name) &&
                             !string.Equals(Name, GetDefaultName(), StringComparison.Ordinal);
        var next = CreateDefault(selectedKind);
        if (keepCustomName)
            next.Name = Name;

        next.Remark = Remark;
        return next;
    }

    private void DrawMetadataFields()
    {
        DrawLabel("名称", KnownColor.LightSkyBlue.ToVector4());
        var name = Name;
        if (ImGui.InputText("###ConditionNameInput", ref name, 128))
            Name = name;

        DrawLabel("备注", KnownColor.LightSkyBlue.ToVector4());
        var remark = Remark;
        if (ImGui.InputText("###ConditionRemarkInput", ref remark, 2048))
            Remark = remark;
    }

    private static ConditionBase InitializeMetadata(ConditionBase condition)
    {
        condition.ResetMetadata();
        return condition;
    }

    internal static ConditionBase CreateDefaultCondition(ConditionDetectType kind) => CreateDefault(kind);

    public static ConditionBase Copy(ConditionBase source) => source.DeepCopy();
}
