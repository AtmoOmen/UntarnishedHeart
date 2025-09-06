using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Dalamud.Game;
using Dalamud.Interface.GameFonts;
using Dalamud.Interface.ManagedFontAtlas;
using Dalamud.Interface.Utility;
using ImGuiNET;

namespace UntarnishedHeart.Managers;

public class FontManager
{
    private static readonly Lazy<IFontAtlas> FontAtlasLazy = 
        new(() => DService.PI.UiBuilder.CreateFontAtlas(FontAtlasAutoRebuildMode.Disable));

    private static readonly Lazy<ushort[]> FontRangeLazy = new(() => BuildRange(null,
        ImGui.GetIO().Fonts.GetGlyphRangesChineseFull(),
        ImGui.GetIO().Fonts.GetGlyphRangesJapanese(),
        ImGui.GetIO().Fonts.GetGlyphRangesKorean(),
        ImGui.GetIO().Fonts.GetGlyphRangesDefault()));

    private static readonly Lazy<IFontHandle> DefaultFontLazy = new(() =>
        FontAtlas.NewGameFontHandle(new(GameFontFamilyAndSize.Axis18)));

    private static IFontHandle? uiFont;
    
    public static IFontAtlas FontAtlas => FontAtlasLazy.Value;
    public static ushort[] FontRange => FontRangeLazy.Value;
    
    public static IFontHandle UIFont => uiFont ?? DefaultFontLazy.Value;
    
    private static string DefaultFontPath => Path.Join(DService.PI.DalamudAssetDirectory.FullName, "UIRes",
                                                     DService.ClientState.ClientLanguage == (ClientLanguage)4 ? 
                                                         "NotoSansCJKsc-Medium.otf" : "NotoSansCJKjp-Medium.otf");

    internal static void Init() => Task.Run(async () => uiFont = await CreateFontHandleAsync(20f));

    private static async Task<IFontHandle> CreateFontHandleAsync(float size)
    {
        var fontPath = DefaultFontPath;
        IFontHandle handle;

        if (!File.Exists(fontPath))
        {
            handle = FontAtlas.NewDelegateFontHandle(e =>
            {
                e.OnPreBuild(tk =>
                {
                    var fileFontPtr = tk.AddDalamudDefaultFont(size, FontRange);

                    var mixedFontPtr0 = tk.AddGameSymbol(new()
                    {
                        SizePx     = size,
                        PixelSnapH = true,
                        MergeFont  = fileFontPtr,
                    });

                    tk.AddFontAwesomeIconFont(new()
                    {
                        SizePx     = size,
                        PixelSnapH = true,
                        MergeFont  = mixedFontPtr0,
                    });
                });
            });
        }
        else
        {
            handle = FontAtlas.NewDelegateFontHandle(e =>
            {
                e.OnPreBuild(tk =>
                {
                    var fileFontPtr = tk.AddFontFromFile(fontPath, new()
                    {
                        SizePx      = size,
                        PixelSnapH  = true,
                        GlyphRanges = FontRange,
                        FontNo      = 0,
                    });

                    var mixedFontPtr0 = tk.AddGameSymbol(new()
                    {
                        SizePx     = size,
                        PixelSnapH = true,
                        MergeFont  = fileFontPtr,
                    });

                    tk.AddFontAwesomeIconFont(new()
                    {
                        SizePx     = size,
                        PixelSnapH = true,
                        MergeFont  = mixedFontPtr0,
                    });
                });
            });
        }

        await FontAtlas.BuildFontsAsync();
        return handle;
    }

    private static unsafe ushort[] BuildRange(IReadOnlyList<ushort>? chars, params nint[] ranges)
    {
        var builder = new ImFontGlyphRangesBuilderPtr(ImGuiNative.ImFontGlyphRangesBuilder_ImFontGlyphRangesBuilder());
        foreach (var range in ranges)
            builder.AddRanges(range);

        if (chars != null)
        {
            for (var i = 0; i < chars.Count; i += 2)
            {
                if (chars[i] == 0)
                    break;

                for (var j = (uint)chars[i]; j <= chars[i + 1]; j++)
                    builder.AddChar((ushort)j);
            }
        }

        builder.AddText("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz");
        builder.AddText("ΑαΒβΓγΔδΕεΖζΗηΘθΙιΚκΛλΜμΝνΞξΟοΠπΡρΣσΤτΥυΦφΧχΨψΩω←→↑↓《》■※☀★★☆♥♡ヅツッシ☀☁☂℃℉°♀♂♠♣♦♣♧®©™€$£♯♭♪✓√◎◆◇♦■□〇●△▽▼▲‹›≤≥<«─＼～⅓½¼⅔¾≠");
        builder.AddText("ŒœĂăÂâÎîȘșȚț");

        for (var i = 0x2460; i <= 0x24B5; i++)
            builder.AddChar((char)i);

        builder.AddChar('⓪');
        return builder.BuildRangesToArray();
    }
    
    internal static void Uninit() => uiFont = null;
}

