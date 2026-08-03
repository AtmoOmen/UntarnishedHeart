using System.IO.Compression;
using System.Text;
using Newtonsoft.Json;
using PresetModel = UntarnishedHeart.Execution.Preset.Preset;

namespace UntarnishedHeart.Execution.Route.Package;

internal static class UTHPackageIO
{
    private const string ManifestFileName = "manifest.json";
    private const string RouteFileName    = "route.json";
    private const string PresetDirectory  = "presets";

    public static void Export
    (
        Route                      route,
        IReadOnlyList<PresetModel> presets,
        string                     filePath
    )
    {
        var manifest = new UTHPackage
        {
            RouteFile   = RouteFileName,
            PresetFiles = presets.Select(preset => $"{PresetDirectory}/{preset.ID}.json").ToList()
        };

        using var stream  = new FileStream(filePath, FileMode.Create, FileAccess.Write);
        using var archive = new ZipArchive(stream, ZipArchiveMode.Create);

        WriteEntry(archive, ManifestFileName, JsonConvert.SerializeObject(manifest, Formatting.Indented));
        WriteEntry(archive, RouteFileName,    JsonConvert.SerializeObject(route,    Formatting.Indented));

        foreach (var preset in presets)
            WriteEntry(archive, $"{PresetDirectory}/{preset.ID}.json", JsonConvert.SerializeObject(preset, Formatting.Indented));
    }

    public static UTHPackageContent Read
    (
        string filePath
    )
    {
        using var archive = ZipFile.OpenRead(filePath);
        var manifest = JsonConvert.DeserializeObject<UTHPackage>(ReadEntry(archive, ManifestFileName)) ??
                       throw new InvalidOperationException("路线包清单无效");

        var route = JsonConvert.DeserializeObject<Route>(ReadEntry(archive, manifest.RouteFile)) ??
                    throw new InvalidOperationException("路线包内容无效");

        var presets = new List<PresetModel>();

        foreach (var presetFile in manifest.PresetFiles)
        {
            var preset = JsonConvert.DeserializeObject<PresetModel>(ReadEntry(archive, presetFile)) ??
                         throw new InvalidOperationException($"路线包预设内容无效: {presetFile}");
            presets.Add(preset);
        }

        return new UTHPackageContent(route, presets);
    }

    private static void WriteEntry
    (
        ZipArchive archive,
        string     entryName,
        string     content
    )
    {
        var       entry  = archive.CreateEntry(entryName);
        using var writer = new StreamWriter(entry.Open(), new UTF8Encoding(false));
        writer.Write(content);
    }

    private static string ReadEntry
    (
        ZipArchive archive,
        string     entryName
    )
    {
        var entry = archive.GetEntry(entryName) ??
                    throw new InvalidOperationException($"路线包缺少文件: {entryName}");
        using var reader = new StreamReader(entry.Open(), Encoding.UTF8);
        return reader.ReadToEnd();
    }
}
