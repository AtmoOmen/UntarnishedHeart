using System.Linq;
using Dalamud.Interface.Windowing;
using UntarnishedHeart.Windows;

namespace UntarnishedHeart.Managers;

public class WindowManager
{
    public static WindowSystem? WindowSystem { get; private set; }

    internal void Init()
    {
        WindowSystem ??= new WindowSystem(PluginName);
        WindowSystem.RemoveAllWindows();
        
        InternalWindows.Init();
        
        DService.UiBuilder.Draw       += DrawWindows;
        DService.UiBuilder.OpenMainUi += ToggleMainWindow;
    }

    private static void DrawWindows()
    {
        using var font = FontManager.UIFont.Push();
        WindowSystem?.Draw();
    }

    private static void ToggleMainWindow()
    {
        var main = Get<Main>();
        if (main == null) return;
        
        main.IsOpen ^= true;
    }

    public static bool AddWindow(Window? window)
    {
        if (WindowSystem == null || window == null) return false;

        var addedWindows = WindowSystem.Windows;
        if (addedWindows.Contains(window) || addedWindows.Any(x => x.WindowName == window.WindowName))
            return false;

        WindowSystem.AddWindow(window);
        return true;
    }

    public static bool RemoveWindow(Window? window)
    {
        if (WindowSystem == null || window == null) return false;

        var addedWindows = WindowSystem.Windows;
        if (!addedWindows.Contains(window)) return false;

        WindowSystem.RemoveWindow(window);
        return true;
    }

    public static T? Get<T>() where T : Window
        => WindowSystem?.Windows.FirstOrDefault(x => x.GetType() == typeof(T)) as T;

    internal void Uninit()
    {
        DService.UiBuilder.Draw       -= DrawWindows;
        DService.UiBuilder.OpenMainUi -= ToggleMainWindow;
        
        InternalWindows.Uninit();
        
        WindowSystem?.RemoveAllWindows();
        WindowSystem = null;
    }

    private static class InternalWindows
    {
        internal static void Init()
        {
            AddWindow(new Main());
            AddWindow(new PresetEditor());
            AddWindow(new RouteEditor());
            AddWindow(new Debug());
        }

        internal static void Uninit()
        {
            Get<Main>()?.Dispose();
            Get<PresetEditor>()?.Dispose();
            Get<RouteEditor>()?.Dispose();
            Get<Debug>()?.Dispose();
        }
    }
}
