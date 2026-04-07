using System.ComponentModel;

namespace UntarnishedHeart.Execution.ExecuteAction.Enums;

public enum ExecuteActionKind
{
    [Description("等待固定时间")]
    Wait,

    [Description("跳转步骤")]
    JumpToStep,

    [Description("重新开始当前步骤")]
    RestartCurrentStep,

    [Description("跳转执行动作")]
    JumpToAction,

    [Description("重新开始当前执行动作")]
    RestartCurrentAction,

    [Description("退出副本并结束预设/路线")]
    LeaveDutyAndEndPreset,

    [Description("退出副本并重新开始预设/路线")]
    LeaveDutyAndRestartPreset,

    [Description("文本指令")]
    TextCommand,

    [Description("选中特定目标")]
    SelectTarget,

    [Description("交互特定目标")]
    InteractTarget,

    [Description("交互附近最近可交互物体")]
    InteractNearestObject,

    [Description("使用技能")]
    UseAction,

    [Description("移动到指定位置")]
    MoveToPosition,

    [Description("界面回调")]
    AddonCallback,

    [Description("代理事件")]
    AgentReceiveEvent
}
