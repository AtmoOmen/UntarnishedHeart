using System;
using Newtonsoft.Json;

namespace UntarnishedHeart.Executor;

/// <summary>
///     路线步骤
/// </summary>
public class RouteStep : IEquatable<RouteStep>
{
    /// <summary>
    ///     步骤名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    ///     步骤类型
    /// </summary>
    public RouteStepType StepType { get; set; } = RouteStepType.SwitchPreset;

    /// <summary>
    ///     备注
    /// </summary>
    public string Remark { get; set; } = string.Empty;

    // 切换预设相关字段
    /// <summary>
    ///     预设名称（用于切换预设步骤）
    /// </summary>
    public string PresetName { get; set; } = string.Empty;

    /// <summary>
    ///     副本选项配置（用于切换预设步骤）
    /// </summary>
    public DutyOptions DutyOptions { get; set; } = new();

    /// <summary>
    ///     预设执行结束后的动作（用于切换预设步骤）
    /// </summary>
    public RouteStepActionType AfterPresetAction { get; set; } = RouteStepActionType.GoToNextStep;

    /// <summary>
    ///     预设执行结束后跳转的目标步骤索引（当AfterPresetAction为JumpToStep时使用）
    /// </summary>
    public int AfterPresetJumpIndex { get; set; }

    // 条件判断相关字段
    /// <summary>
    ///     条件类型（用于条件判断步骤）
    /// </summary>
    public ConditionType ConditionType { get; set; } = ConditionType.PlayerLevel;

    /// <summary>
    ///     比较类型（用于条件判断步骤）
    /// </summary>
    public ComparisonType ComparisonType { get; set; } = ComparisonType.GreaterThan;

    /// <summary>
    ///     条件值（用于条件判断步骤）
    /// </summary>
    public int ConditionValue { get; set; }

    /// <summary>
    ///     额外ID（用于成就数和物品数条件）
    /// </summary>
    public int ExtraId { get; set; }

    /// <summary>
    ///     条件满足时的执行逻辑
    /// </summary>
    public RouteStepActionType TrueAction { get; set; } = RouteStepActionType.RepeatCurrentStep;

    /// <summary>
    ///     条件满足时跳转的步骤索引（当TrueAction为JumpToStep时使用）
    /// </summary>
    public int TrueJumpIndex { get; set; }

    /// <summary>
    ///     条件不满足时的执行逻辑
    /// </summary>
    public RouteStepActionType FalseAction { get; set; } = RouteStepActionType.RepeatCurrentStep;

    /// <summary>
    ///     条件不满足时跳转的步骤索引（当FalseAction为JumpToStep时使用）
    /// </summary>
    public int FalseJumpIndex { get; set; }

    /// <summary>
    ///     步骤是否有效
    /// </summary>
    [JsonIgnore] public bool IsValid
    {
        get =>
            StepType switch
            {
                RouteStepType.SwitchPreset   => !string.IsNullOrEmpty(PresetName),
                RouteStepType.ConditionCheck => true, // 条件判断步骤总是有效的
                _                            => false
            };
    }

    public bool Equals(RouteStep? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;

        return Name       == other.Name                           &&
               StepType   == other.StepType                       &&
               Remark     == other.Remark                         &&
               PresetName == other.PresetName                     &&
               DutyOptions.Equals(other.DutyOptions)              &&
               AfterPresetAction    == other.AfterPresetAction    &&
               AfterPresetJumpIndex == other.AfterPresetJumpIndex &&
               ConditionType        == other.ConditionType        &&
               ComparisonType       == other.ComparisonType       &&
               ConditionValue       == other.ConditionValue       &&
               ExtraId              == other.ExtraId              &&
               TrueAction           == other.TrueAction           &&
               TrueJumpIndex        == other.TrueJumpIndex        &&
               FalseAction          == other.FalseAction          &&
               FalseJumpIndex       == other.FalseJumpIndex;
    }

    /// <summary>
    ///     复制步骤
    /// </summary>
    /// <param name="source">源步骤</param>
    /// <returns>复制的步骤</returns>
    public static RouteStep Copy(RouteStep? source)
    {
        if (source == null) return new RouteStep();

        return new RouteStep
        {
            Name       = source.Name,
            StepType   = source.StepType,
            Remark     = source.Remark,
            PresetName = source.PresetName,
            DutyOptions = new DutyOptions
            {
                LeaderMode           = source.DutyOptions.LeaderMode,
                AutoRecommendGear    = source.DutyOptions.AutoRecommendGear,
                RunTimes             = source.DutyOptions.RunTimes,
                ContentEntryType     = source.DutyOptions.ContentEntryType,
                ContentsFinderOption = source.DutyOptions.ContentsFinderOption.Clone()
            },
            AfterPresetAction    = source.AfterPresetAction,
            AfterPresetJumpIndex = source.AfterPresetJumpIndex,
            ConditionType        = source.ConditionType,
            ComparisonType       = source.ComparisonType,
            ConditionValue       = source.ConditionValue,
            ExtraId              = source.ExtraId,
            TrueAction           = source.TrueAction,
            TrueJumpIndex        = source.TrueJumpIndex,
            FalseAction          = source.FalseAction,
            FalseJumpIndex       = source.FalseJumpIndex
        };
    }

    public override bool Equals(object? obj) => Equals(obj as RouteStep);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(Name);
        hash.Add(StepType);
        hash.Add(Remark);
        hash.Add(PresetName);
        hash.Add(DutyOptions);
        hash.Add(AfterPresetAction);
        hash.Add(AfterPresetJumpIndex);
        hash.Add(ConditionType);
        hash.Add(ComparisonType);
        hash.Add(ConditionValue);
        hash.Add(ExtraId);
        hash.Add(TrueAction);
        hash.Add(TrueJumpIndex);
        hash.Add(FalseAction);
        hash.Add(FalseJumpIndex);
        return hash.ToHashCode();
    }
}

/// <summary>
///     副本选项配置
/// </summary>
public class DutyOptions : IEquatable<DutyOptions>
{
    /// <summary>
    ///     队长模式
    /// </summary>
    public bool LeaderMode { get; set; }

    /// <summary>
    ///     自动最强
    /// </summary>
    public bool AutoRecommendGear { get; set; }

    /// <summary>
    ///     运行次数
    /// </summary>
    public int RunTimes { get; set; } = 1;

    /// <summary>
    ///     副本入口类型
    /// </summary>
    public ContentEntryType ContentEntryType { get; set; } = ContentEntryType.Normal;

    /// <summary>
    ///     副本查找选项
    /// </summary>
    public ContentsFinderOption ContentsFinderOption { get; set; } = ContentsFinderHelper.DefaultOption.Clone();

    public bool Equals(DutyOptions? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;

        return LeaderMode        == other.LeaderMode        &&
               AutoRecommendGear == other.AutoRecommendGear &&
               RunTimes          == other.RunTimes          &&
               ContentEntryType  == other.ContentEntryType  &&
               ContentsFinderOption.Equals(other.ContentsFinderOption);
    }

    public override bool Equals(object? obj) => Equals(obj as DutyOptions);

    public override int GetHashCode() => HashCode.Combine(LeaderMode, AutoRecommendGear, RunTimes, ContentEntryType, ContentsFinderOption);
}
