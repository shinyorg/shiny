using System;
using UserNotifications;


namespace Shiny.Notifications
{
    public class ShinyNotificationDelegate : UNUserNotificationCenterDelegate
    {
        public override async void DidReceiveNotificationResponse(UNUserNotificationCenter center, UNNotificationResponse response, Action completionHandler)
        {
            var processor = ShinyHost.Resolve<NotificationProcessor>();
            if (processor != null)
                await processor.Entry(response.Notification.Request.Identifier);

            completionHandler();
        }


        public override async void WillPresentNotification(UNUserNotificationCenter center, UNNotification notification, Action<UNNotificationPresentationOptions> completionHandler)
        {
            var processor = ShinyHost.Resolve<NotificationProcessor>();
            if (processor != null)
                await processor.Receive(notification.Request.Identifier);

            completionHandler(UNNotificationPresentationOptions.Alert);
        }
    }
}
