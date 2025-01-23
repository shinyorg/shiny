using System;
using UserNotifications;

namespace Shiny;


public class ShinyUNUserNotificationCenterDelegate : UNUserNotificationCenterDelegate
{
    readonly Action<UNNotificationResponse, Action> onReceive;
    readonly Action<UNNotification, Action<UNNotificationPresentationOptions>> onWillPresent;

    public ShinyUNUserNotificationCenterDelegate(
        Action<UNNotificationResponse, Action> onReceive,
        Action<UNNotification, Action<UNNotificationPresentationOptions>> onWillPresent
    )
    {
        this.onReceive = onReceive;
        this.onWillPresent = onWillPresent;
    }

    public override void DidReceiveNotificationResponse(UNUserNotificationCenter center, UNNotificationResponse response, Action completionHandler)
        => this.onReceive(response, completionHandler);

    public override void WillPresentNotification(UNUserNotificationCenter center, UNNotification notification, Action<UNNotificationPresentationOptions> completionHandler)
        => this.onWillPresent(notification, completionHandler);
}
