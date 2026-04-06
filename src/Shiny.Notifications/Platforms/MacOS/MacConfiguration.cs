using UserNotifications;

namespace Shiny.Notifications;


public record MacConfiguration(
    UNAuthorizationOptions UNAuthorizationOptions =
        UNAuthorizationOptions.Alert |
        UNAuthorizationOptions.Badge |
        UNAuthorizationOptions.Sound,

    UNNotificationPresentationOptions PresentationOptions =
        UNNotificationPresentationOptions.Banner |
        UNNotificationPresentationOptions.Badge |
        UNNotificationPresentationOptions.Sound
);
