using System.Linq;
using Dalamud.Interface.Windowing;
using UntarnishedHeart.Windows;

namespace UntarnishedHeart.Managers;

public class WindowManager
{
    private static WindowSystem? _windowSystem;
    private static Main? _main;

    private static readonly object _windowLock = new();

    public static WindowSystem? WindowSystem => _windowSystem;
    public static Main?         Main         => _main;

    internal void Init()
    {
        _windowSystem = new WindowSystem(PluginName);
        _windowSystem.RemoveAllWindows();

        _main = new Main();
        AddWindows(_main);

        DService.UiBuilder.Draw += DrawWindows;
        DService.UiBuilder.OpenMainUi += ToggleMainWindow;
    }

    private static void DrawWindows() => _windowSystem?.Draw();

    private static void ToggleMainWindow()
    {
        if (_main != null)
            _main.IsOpen ^= true;
    }

    public bool AddWindows(Window? window)
    {
        if (window == null || _windowSystem == null)
            return false;

        lock (_windowLock)
        {
            var addedWindows = _windowSystem.Windows;

            if (addedWindows.Contains(window) || addedWindows.Any(x => x.WindowName == window.WindowName))
                return false;

            _windowSystem.AddWindow(window);
            return true;
        }
    }

    public bool RemoveWindows(Window? window)
    {
        if (window == null || _windowSystem == null)
            return false;

        lock (_windowLock)
        {
            var addedWindows = _windowSystem.Windows;

            if (!addedWindows.Contains(window))
                return false;

            _windowSystem.RemoveWindow(window);
            return true;
        }
    }

    internal void Uninit()
    {
        DService.UiBuilder.Draw -= DrawWindows;
        DService.UiBuilder.OpenMainUi -= ToggleMainWindow;

        _windowSystem?.RemoveAllWindows();
        _windowSystem = null;

        _main?.Dispose();
        _main = null;
    }
}
