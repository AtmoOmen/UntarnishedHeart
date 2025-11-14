using System;
using System.Collections.Generic;
using System.Linq;
using Lumina.Excel.Sheets;

namespace UntarnishedHeart.Utils;

public static class PresetData
{
    public static Dictionary<uint, ContentFinderCondition> Contents => contents.Value;
    
    private static readonly Lazy<Dictionary<uint, ContentFinderCondition>> contents =
        new(() => LuminaGetter.Get<ContentFinderCondition>()
                             .Where(x => !string.IsNullOrWhiteSpace(x.Name.ExtractText()))
                             .DistinctBy(x => x.TerritoryType.RowId)
                             .OrderBy(x => x.ContentType.RowId)
                             .ThenBy(x => x.ClassJobLevelRequired)
                             .ToDictionary(x => x.TerritoryType.RowId, x => x));
}
