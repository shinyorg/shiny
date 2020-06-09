using System;
using UserNotifications;

namespace Shiny.Notifications
{
    public class PresentArgs
    {
        public PresentArgs(UNNotification notification, Action<UNNotificationPresentationOptions> completionHandler)
        {
            this.Notification = notification;
            this.CompletionHandler = completionHandler;
        }

        public UNNotification Notification { get; }
        public Action<UNNotificationPresentationOptions> CompletionHandler { get; }
    }
}
