using System.ComponentModel;

namespace UntarnishedHeart.Execution.ExecuteAction.Enums;

public enum SwitchClassJobMode
{
    [Description("按职业")]
    ByClassJob,

    [Description("按套装编号")]
    ByGearsetID
}
