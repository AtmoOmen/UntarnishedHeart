using OmenTools.OmenService;
using UntarnishedHeart.Windows;

namespace UntarnishedHeart.Internal;

internal static class PluginWindow
{
    private static DrawScopesHandle WindowStylesHandle;

    public static void Init()
    {
        var fontManager   = FontManager.Instance();
        var windowManager = WindowManager.Instance();

        WindowStylesHandle = windowManager.RegDrawScopes(() => fontManager.UIFont.Push());

        windowManager.AddWindow<Main>();
        windowManager.AddWindow<PresetEditor>();
        windowManager.AddWindow<RouteEditor>();
        windowManager.AddWindow<Debug>();

        DService.Instance().UIBuilder.OpenMainUi += OnMainUI;
    }

    public static void Uninit()
    {
        var manager = WindowManager.Instance();

        manager.RemoveWindow<Main>();
        manager.RemoveWindow<PresetEditor>();
        manager.RemoveWindow<RouteEditor>();
        manager.RemoveWindow<Debug>();

        manager.UnregDrawScopes(WindowStylesHandle);
        WindowStylesHandle = default;

        DService.Instance().UIBuilder.OpenMainUi -= OnMainUI;
    }

    private static void OnMainUI()
    {
        if (WindowManager.Instance().Get<Main>() is not { } mainWindow)
            return;

        mainWindow.IsOpen ^= true;
    }
}
