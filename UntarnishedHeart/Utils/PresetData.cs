using System;
using System.Collections.Generic;
using System.Linq;
using Lumina.Excel.GeneratedSheets;

namespace UntarnishedHeart.Utils;

public static class PresetData
{
    public static Dictionary<uint, ContentFinderCondition> Contents        => contents.Value;
    
    private static readonly Lazy<Dictionary<uint, ContentFinderCondition>> contents =
        new(() => LuminaCache.Get<ContentFinderCondition>()
                             .Where(x => !string.IsNullOrWhiteSpace(x.Name.ExtractText()))
                             .DistinctBy(x => x.TerritoryType.Row)
                             .OrderBy(x => x.ContentType.Row)
                             .ThenBy(x => x.ClassJobLevelRequired)
                             .ToDictionary(x => x.TerritoryType.Row, x => x));
}
