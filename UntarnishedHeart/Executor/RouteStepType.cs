using System.ComponentModel;

namespace UntarnishedHeart.Executor;

/// <summary>
/// 路线步骤类型
/// </summary>
public enum RouteStepType
{
    [Description("切换预设")]
    SwitchPreset,
    
    [Description("条件判断")]
    ConditionCheck
}
