using System;
using UserNotifications;
using Shiny.Logging;


namespace Shiny.Notifications
{
    public class ShinyNotificationDelegate : UNUserNotificationCenterDelegate
    {
        readonly INotificationDelegate sdelegate;
        public ShinyNotificationDelegate(INotificationDelegate sdelegate)
            => this.sdelegate = sdelegate;


        public override void DidReceiveNotificationResponse(UNUserNotificationCenter center, UNNotificationResponse response, Action completionHandler)
            => Log.SafeExecute(async () =>
            {
                var notification = response.Notification.Request.FromNative();
                if (response is UNTextInputNotificationResponse textResponse)
                {
                    var shinyResponse = new NotificationResponse(
                        notification,
                        textResponse.ActionIdentifier,
                        textResponse.UserText
                    );
                    await this.sdelegate.OnEntry(shinyResponse);
                }
                else
                {
                    var shinyResponse = new NotificationResponse(notification, response.ActionIdentifier, null);
                    await sdelegate.OnEntry(shinyResponse);
                }
            })
            .ContinueWith(_ => completionHandler());


        public override void WillPresentNotification(UNUserNotificationCenter center, UNNotification notification, Action<UNNotificationPresentationOptions> completionHandler)
            => Log.SafeExecute(() =>
                this.sdelegate.OnReceived(notification.Request.FromNative())
            )
            .ContinueWith(_ => completionHandler(UNNotificationPresentationOptions.Alert));
    }
}
