using System;


namespace Shiny.Notifications
{
    //https://docs.microsoft.com/en-us/windows/uwp/design/shell/tiles-and-notifications/toast-progress-bar
    //https://www.tutlane.com/tutorial/android/android-progress-notification-with-examples#:~:text=%20Android%20Progress%20Notification%20with%20Examples%20%201,using%20android%20virtual%20device%20%28AVD%29%20we...%20More%20
    public interface IPersistentNotificationManagerExtension
    {
        IPersistentNotification Create(Notification notification);
    }
}
