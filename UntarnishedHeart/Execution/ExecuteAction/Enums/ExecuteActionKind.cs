using System.ComponentModel;

namespace UntarnishedHeart.Execution.ExecuteAction.Enums;

public enum ExecuteActionKind
{
    [Description("等待固定时间")]
    Wait,

    [Description("跳转步骤")]
    JumpToStep,

    [Description("跳转执行动作")]
    JumpToAction,

    [Description("退出副本并结束预设/路线")]
    LeaveDutyAndEndPreset,

    [Description("退出副本并重新开始预设/路线")]
    LeaveDutyAndRestartPreset,

    [Description("文本指令")]
    TextCommand,

    [Description("游戏命令")]
    GameCommand,

    [Description("游戏命令 (详细参数)")]
    GameCommandComplex,

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

    [Description("向界面发送操作")]
    AddonCallback,

    [Description("向界面发送事件")]
    AgentReceiveEvent,

    [Description("执行预设")]
    ExecutePreset,

    [Description("切换职业")]
    SwitchClassJob
}
