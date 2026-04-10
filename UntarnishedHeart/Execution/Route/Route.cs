using Newtonsoft.Json;
using OmenTools.OmenService;
using UntarnishedHeart.Execution.Preset;
using UntarnishedHeart.Execution.Route.Configuration;

namespace UntarnishedHeart.Execution.Route;

[JsonConverter(typeof(RouteJsonConverter))]
public sealed class Route : IEquatable<Route>
{
    public int Version { get; set; } = RouteJSONMigrator.CurrentJSONVersion;

    public string Name { get; set; } = string.Empty;

    public string Remark { get; set; } = string.Empty;

    public List<PresetStep> Steps { get; set; } = [];

    [JsonIgnore]
    public bool IsValid => !string.IsNullOrWhiteSpace(Name) && Steps.Count > 0;

    public Route Copy() =>
        new()
        {
            Version = Version,
            Name    = Name,
            Remark  = Remark,
            Steps   = Steps.Select(PresetStep.Copy).ToList()
        };

    public void ExportToClipboard()
    {
        try
        {
            var json = JsonConvert.SerializeObject(this, Formatting.Indented);
            ImGui.SetClipboardText(json);
            NotifyHelper.Instance().Chat("路线已导出到剪贴板");
        }
        catch (Exception ex)
        {
            NotifyHelper.Instance().Chat($"导出路线失败: {ex.Message}");
        }
    }

    public static Route? ImportFromClipboard()
    {
        try
        {
            var clipboardText = ImGui.GetClipboardText();
            if (string.IsNullOrWhiteSpace(clipboardText))
                return null;

            return JsonConvert.DeserializeObject<Route>(clipboardText);
        }
        catch (Exception ex)
        {
            NotifyHelper.Instance().Chat($"导入路线失败: {ex.Message}");
            return null;
        }
    }

    public bool Equals(Route? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;

        return Name   == other.Name   &&
               Remark == other.Remark &&
               Steps.SequenceEqual(other.Steps);
    }

    public override bool Equals(object? obj) => Equals(obj as Route);

    public override int GetHashCode() => HashCode.Combine(Name, Remark, Steps.Count);
}
