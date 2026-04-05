using System.ComponentModel;

namespace UntarnishedHeart.Execution.Preset.Enums;

public enum TargetSelectorKind
{
    [Description("当前目标")]
    CurrentTarget,

    [Description("按对象类型和 Data ID")]
    ByObjectKindAndDataID,

    [Description("按 Entity ID")]
    ByEntityID
}
