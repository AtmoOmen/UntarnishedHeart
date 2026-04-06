using Dalamud.Interface.Windowing;
using OmenTools.OmenService;
using UntarnishedHeart.Internal;

namespace UntarnishedHeart.Windows;

public class SettingsWindow() : Window
(
    $"设置###{Plugin.PLUGIN_NAME}-Settings",
    ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoCollapse
)
{
    public override void Draw()
    {
        ImGui.SetNextItemWidth(200f * GlobalUIScale);
        if (ImGui.InputFloat("界面字号###InterfaceFontInput", ref FontManager.Instance().Config.FontSize, 0, 0, "%.1f"))
            FontManager.Instance().Config.FontSize = Math.Clamp(FontManager.Instance().Config.FontSize, 8, 48);

        if (ImGui.IsItemDeactivatedAfterEdit())
        {
            FontManager.Instance().Config.Save();
            _ = FontManager.Instance().RebuildUIFontsAsync();
        }

        var config     = PluginConfig.Instance();
        var unlockSize = config.UnlockMainWindowSize;

        if (ImGui.Checkbox("解锁主界面窗口尺寸", ref unlockSize))
        {
            config.UnlockMainWindowSize = unlockSize;
            config.Save();

            if (WindowManager.Instance().Get<MainWindow>() is { } mainWindow)
                mainWindow.RefreshWindowFlags();
        }

    }
}
