using System.Numerics;
using Newtonsoft.Json;
using UntarnishedHeart.Execution.Condition.Configuration;
using UntarnishedHeart.Execution.Condition.Enums;
using UntarnishedHeart.Execution.Condition.Legacy;
using UntarnishedHeart.Execution.Models;
using UntarnishedHeart.Execution.Preset.Helpers;
using UntarnishedHeart.Windows;

namespace UntarnishedHeart.Execution.Condition;

[JsonObject(MemberSerialization.OptIn)]
[JsonConverter(typeof(ConditionJSONConverter))]
public abstract class ConditionBase : IEquatable<ConditionBase>
{
    protected const float EQUALITY_TOLERANCE = 0.01f;

    [JsonProperty("Name")]
    public string Name { get; set; } = string.Empty;

    [JsonProperty("Remark")]
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

        return Kind   == other.Kind   &&
               Name   == other.Name   &&
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

    internal static TEnum DrawEnumCombo<TEnum>(string id, TEnum current, in HashSet<TEnum>? passedEnums = null)
        where TEnum : struct, Enum
    {
        using var combo = ImRaii.Combo(id, current.GetDescription(), ImGuiComboFlags.HeightLargest);
        if (!combo)
            return current;

        foreach (var candidate in Enum.GetValues<TEnum>())
        {
            if (passedEnums != null && passedEnums.Contains(candidate))
                continue;

            if (ImGui.Selectable(candidate.GetDescription(), EqualityComparer<TEnum>.Default.Equals(current, candidate)))
                current = candidate;

            ImGuiOm.TooltipHover(candidate.GetDescription());
        }

        return current;
    }

    internal static void DrawEnumLocalizedSelector<TEnum>
    (
        string                id,
        string                title,
        string                emptyText,
        TEnum                 current,
        Action<TEnum>         setCurrent,
        Func<TEnum, string>   getDisplayText,
        Func<TEnum, string?>? getDescription = null,
        in HashSet<TEnum>?    passedEnums    = null
    )
        where TEnum : struct, Enum
    {
        var skippedEnums = passedEnums;
        var candidates = Enum
                         .GetValues<TEnum>()
                         .Where(candidate => skippedEnums == null || !skippedEnums.Contains(candidate))
                         .ToArray();

        if (candidates.Length == 0)
        {
            ImGui.TextDisabled(emptyText);
            return;
        }

        using var combo = ImRaii.Combo(id, getDisplayText(current), ImGuiComboFlags.HeightLarge);
        if (combo)
            ImGui.CloseCurrentPopup();

        if (!ImGui.IsItemClicked())
            return;

        var request = new CollectionSelectorRequest
        (
            title,
            emptyText,
            Array.IndexOf(candidates, current),
            candidates.Select(candidate => new CollectionSelectorItem(getDisplayText(candidate), getDescription?.Invoke(candidate))).ToArray()
        );

        CollectionSelectorWindow.Open
        (
            request,
            index =>
            {
                if ((uint)index >= (uint)candidates.Length)
                    return;

                setCurrent(candidates[index]);
            }
        );
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
        ConditionJsonTypeRegistry.Instance.CreateDefault(kind);

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
                ConditionDetectType.ActionCast => new ActionCastCondition
                {
                    TargetType     = targetType,
                    ComparisonType = comparisonType == ConditionComparisonType.NotHas ? PresenceComparisonType.NotHas : PresenceComparisonType.Has,
                    Action         = new ActionReference { ActionID = (uint)Math.Max(0, value) }
                },
                _ => new HealthCondition()
            }
        );

    private static ConditionBase InitializeMetadata(ConditionBase condition)
    {
        condition.ResetMetadata();
        return condition;
    }

    private ConditionBase DrawKindSelector()
    {
        DrawLabel("条件类型", KnownColor.LightSkyBlue.ToVector4());

        var selectedKind = DrawEnumCombo("###ConditionKindCombo", Kind, [ConditionDetectType.ActionCastStart]);
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

    internal static ConditionBase CreateDefaultCondition(ConditionDetectType kind) => CreateDefault(kind);

    public static ConditionBase Copy(ConditionBase source) => source.DeepCopy();
}
