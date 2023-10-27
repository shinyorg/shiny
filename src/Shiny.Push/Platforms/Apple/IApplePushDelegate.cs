using UIKit;
using UserNotifications;

namespace Shiny.Push;


public interface IApplePushDelegate : IPushDelegate
{
    /// <summary>
    /// Get the presentation options for a push notification when in the foreground
    /// Will default to UNNotificationPresentationOptions.List | UNNotificationPresentationOptions.Banner if null is returned
    /// Returning null will also allow any other ApplePushDelegates registered to present
    /// </summary>
    /// <param name="notification"></param>
    /// <returns></returns>
    UNNotificationPresentationOptions? GetPresentationOptions(PushNotification notification);

    /// <summary>
    /// Get the fetch result for a notification
    /// Will default to UIBackgroundFetchResult.NewData if null is returned
    /// Returning null will also allow any other ApplePushDelegates registered to interact
    /// </summary>
    /// <param name="notification"></param>
    /// <returns></returns>
    UIBackgroundFetchResult? GetFetchResult(PushNotification notification);
}