using System;
using UserNotifications;
using Shiny.Logging;


namespace Shiny.Notifications
{
    public class ShinyNotificationDelegate : UNUserNotificationCenterDelegate
    {
        public override void DidReceiveNotificationResponse(UNUserNotificationCenter center, UNNotificationResponse response, Action completionHandler)
            => Log.SafeExecute(async () =>
            {
                var notification = response.Notification.Request.FromNative();
                if (notification == null)
                    return;

                var sdelegate = ShinyHost.Resolve<INotificationDelegate>();
                if (x.Response is UNTextInputNotificationResponse textResponse)
                {
                    var shinyResponse = new NotificationResponse(notification, textResponse.ActionIdentifier, textResponse.UserText);
                    await sdelegate.OnEntry(shinyResponse);
                }
                else
                {
                    var shinyResponse = new NotificationResponse(notification, x.Response.ActionIdentifier, null);
                    await sdelegate.OnEntry(shinyResponse);
                }
            })
            .ContinueWith(_ => completionHandler());


        public override void WillPresentNotification(UNUserNotificationCenter center, UNNotification notification, Action<UNNotificationPresentationOptions> completionHandler)
            => Log.SafeExecute(async () =>
            {
                var shinyNotification = notification.Request.FromNative();
                if (shinyNotification != null)
                {
                    await ShinyHost
                        .Resolve<INotificationDelegate>()
                        .OnReceived(shinyNotification);
                }
            })
            .ContinueWith(_ => completionHandler(UNNotificationPresentationOptions.Alert));
    }
}
