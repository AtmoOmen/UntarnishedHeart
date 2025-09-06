using System;
using System.Collections.Generic;
using System.Linq;

namespace UntarnishedHeart.Executor;

/// <summary>
/// 运行路线
/// </summary>
[Serializable]
public class Route
{
    /// <summary>
    /// 路线名称
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// 路线备注
    /// </summary>
    public string Note { get; set; } = string.Empty;
    
    /// <summary>
    /// 路线步骤列表
    /// </summary>
    public List<RouteStep> Steps { get; set; } = new();
    
    /// <summary>
    /// 路线是否有效
    /// </summary>
    public bool IsValid => !string.IsNullOrWhiteSpace(Name) && Steps.Any(s => s.IsValid);
    
    /// <summary>
    /// 复制路线
    /// </summary>
    public Route Copy()
    {
        return new Route
        {
            Name = Name,
            Note = Note,
            Steps = Steps.Select(s => RouteStep.Copy(s)).ToList()
        };
    }
    
    /// <summary>
    /// 判断路线是否相等
    /// </summary>
    public override bool Equals(object? obj)
    {
        if (obj is not Route other) return false;
        
        return Name == other.Name &&
               Note == other.Note &&
               Steps.SequenceEqual(other.Steps);
    }
    
    /// <summary>
    /// 获取哈希码
    /// </summary>
    public override int GetHashCode()
    {
        return HashCode.Combine(Name, Note, Steps.Count);
    }
}