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

        windowManager.AddWindow<MainWindow>();
        windowManager.AddWindow<SettingsWindow>();
        windowManager.AddWindow<ExecutionStatusWindow>();
        windowManager.AddWindow<CollectionSelectorWindow>();
        windowManager.AddWindow<PresetEditor>();
        windowManager.AddWindow<RouteEditor>();
        windowManager.AddWindow<DebugWindow>();

        DService.Instance().UIBuilder.OpenMainUi += OnMainUI;
    }

    public static void Uninit()
    {
        var manager = WindowManager.Instance();

        manager.RemoveWindow<MainWindow>();
        manager.RemoveWindow<SettingsWindow>();
        manager.RemoveWindow<ExecutionStatusWindow>();
        manager.RemoveWindow<CollectionSelectorWindow>();
        manager.RemoveWindow<PresetEditor>();
        manager.RemoveWindow<RouteEditor>();
        manager.RemoveWindow<DebugWindow>();

        manager.UnregDrawScopes(WindowStylesHandle);
        WindowStylesHandle = default;

        DService.Instance().UIBuilder.OpenMainUi -= OnMainUI;
    }

    private static void OnMainUI()
    {
        if (WindowManager.Instance().Get<MainWindow>() is not { } mainWindow)
            return;

        mainWindow.IsOpen ^= true;
    }
}
