using UserNotifications;

namespace Shiny.Push;


public interface IApplePushDelegate : IPushDelegate
{
    /// <summary>
    /// Get the presentation options for a push notification when in the foreground
    /// Will default to UNNotificationPresentationOptions.List | UNNotificationPresentationOptions.Banner if null is returned
    /// Returning null will also allow any other ApplePushDelegates registered to present
    /// </summary>
    UNNotificationPresentationOptions? GetPresentationOptions(PushNotification notification);
}
