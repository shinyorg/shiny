using System;
using Shiny.Logging;
using UserNotifications;


namespace Shiny.Notifications
{
    public class ShinyNotificationDelegate : UNUserNotificationCenterDelegate
    {
        readonly Lazy<INotificationDelegate> sdelegate = new Lazy<INotificationDelegate>(() => ShinyHost.Resolve<INotificationDelegate>());


        public override async void DidReceiveNotificationResponse(UNUserNotificationCenter center, UNNotificationResponse response, Action completionHandler)
        {
            if (this.sdelegate.Value != null)
            {
                var notification = response.Notification.Request.FromNative();
                if (response is UNTextInputNotificationResponse textResponse)
                {
                    await Log.SafeExecute(async () =>
                    {
                        var shinyResponse = new NotificationResponse(notification, textResponse.ActionIdentifier, textResponse.UserText);
                        await this.sdelegate.Value.OnEntry(shinyResponse);
                    });
                }
                else
                {
                    await Log.SafeExecute(async () =>
                    {
                        var shinyResponse = new NotificationResponse(notification, response.ActionIdentifier, null);
                        await this.sdelegate.Value.OnEntry(shinyResponse);
                    });
                }
            }
            completionHandler();
        }


        public override async void WillPresentNotification(UNUserNotificationCenter center, UNNotification notification, Action<UNNotificationPresentationOptions> completionHandler)
        {
            if (this.sdelegate.Value != null)
            {
                await Log.SafeExecute(async () =>
                {
                    var shinyNotification = notification.Request.FromNative();
                    await this.sdelegate.Value.OnReceived(shinyNotification);
                });
            }
            completionHandler(UNNotificationPresentationOptions.Alert);
        }
    }
}
