using Dalamud.Interface.ImGuiNotification;
using System;

namespace UntarnishedHeart.Utils;

public static class NotifyHelper
{
    public static void NotificationSuccess(string message, string? title = null) => DService.DNotice.AddNotification(new()
    {
        Title = title ?? message,
        Content = message,
        Type = NotificationType.Success,
        Minimized = false,
        InitialDuration = TimeSpan.FromSeconds(3),
        ExtensionDurationSinceLastInterest = TimeSpan.FromSeconds(1),
    });

    public static void NotificationWarning(string message, string? title = null) => DService.DNotice.AddNotification(new()
    {
        Title = title ?? message,
        Content = message,
        Type = NotificationType.Warning,
        Minimized = false,
        InitialDuration = TimeSpan.FromSeconds(3),
        ExtensionDurationSinceLastInterest = TimeSpan.FromSeconds(1),
    });

    public static void NotificationError(string message, string? title = null) => DService.DNotice.AddNotification(new()
    {
        Title = title ?? message,
        Content = message,
        Type = NotificationType.Error,
        Minimized = false,
        InitialDuration = TimeSpan.FromSeconds(3),
        ExtensionDurationSinceLastInterest = TimeSpan.FromSeconds(1),
    });

    public static void NotificationInfo(string message, string? title = null) => DService.DNotice.AddNotification(new()
    {
        Title = title ?? message,
        Content = message,
        Type = NotificationType.Info,
        Minimized = false,
        InitialDuration = TimeSpan.FromSeconds(3),
        ExtensionDurationSinceLastInterest = TimeSpan.FromSeconds(1),
    });
}
