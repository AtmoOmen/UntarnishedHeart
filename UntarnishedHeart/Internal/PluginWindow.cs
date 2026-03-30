using OmenTools.OmenService;
using UntarnishedHeart.Windows;

namespace UntarnishedHeart.Internal;

internal static class PluginWindow
{
    public static void Init()
    {
        var manager = WindowManager.Instance();

        manager.AddWindow<Main>();
        manager.AddWindow<PresetEditor>();
        manager.AddWindow<RouteEditor>();
        manager.AddWindow<Debug>();

        DService.Instance().UIBuilder.OpenMainUi += OnMainUI;
    }

    public static void Uninit()
    {
        var manager = WindowManager.Instance();

        manager.RemoveWindow<Main>();
        manager.RemoveWindow<PresetEditor>();
        manager.RemoveWindow<RouteEditor>();
        manager.RemoveWindow<Debug>();
    }

    private static void OnMainUI()
    {
        if (WindowManager.Instance().Get<Main>() is not { } mainWindow)
            return;

        mainWindow.IsOpen ^= true;
    }
}
