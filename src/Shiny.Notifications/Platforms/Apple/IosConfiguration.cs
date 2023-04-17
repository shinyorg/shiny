using UserNotifications;

namespace Shiny.Notifications;


public record IosConfiguration(
    UNAuthorizationOptions UNAuthorizationOptions =
        UNAuthorizationOptions.Alert |
        UNAuthorizationOptions.Badge |
        UNAuthorizationOptions.Sound,

    UNNotificationPresentationOptions PresentationOptions =
        UNNotificationPresentationOptions.Banner |
        UNNotificationPresentationOptions.Badge |
        UNNotificationPresentationOptions.Sound 
);
