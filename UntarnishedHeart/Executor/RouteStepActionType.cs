namespace UntarnishedHeart.Executor;

using System.ComponentModel;

/// <summary>
/// 执行逻辑类型
/// </summary>
public enum RouteStepActionType
{
    [Description("重复步骤")]
    RepeatCurrentStep,
    
    [Description("跳转步骤")]
    JumpToStep,
    
    [Description("结束路线")]
    EndRoute,
    
    [Description("回到上一步")]
    GoToPreviousStep,
    
    [Description("顺延下一步")]
    GoToNextStep
}
