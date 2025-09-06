namespace UntarnishedHeart.Executor;

using System.ComponentModel;

/// <summary>
/// 比较类型
/// </summary>
public enum ComparisonType
{
    [Description("＞")]
    GreaterThan,
    
    [Description("＜")]
    LessThan,
    
    [Description("＝")]
    Equal,
    
    [Description("≥")]
    GreaterThanOrEqual,
    
    [Description("≤")]
    LessThanOrEqual,
    
    [Description("≠")]
    NotEqual
}
