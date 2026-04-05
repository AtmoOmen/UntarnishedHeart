using System.ComponentModel;

namespace UntarnishedHeart.Execution.Preset.Enums;

public enum PresetStepPhase
{
    [Description("步骤进入时")]
    Enter,

    [Description("步骤进行时")]
    Body,

    [Description("步骤离开时")]
    Exit
}
