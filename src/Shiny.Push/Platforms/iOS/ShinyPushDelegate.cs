using System;
using Shiny.Logging;
using UserNotifications;


namespace Shiny.Push
{
    public class ShinyPushDelegate : UNUserNotificationCenterDelegate
    {
        readonly Lazy<IPushDelegate> sdelegate = new Lazy<IPushDelegate>(() => ShinyHost.Resolve<IPushDelegate>());


        public override async void DidReceiveNotificationResponse(UNUserNotificationCenter center, UNNotificationResponse response, Action completionHandler)
        {
            
            //var notification = response.Notification.Request.FromNative();
            //if (response is UNTextInputNotificationResponse textResponse)
            //{
            //    await Log.SafeExecute(async () =>
            //    {
            //        var shinyResponse = new NotificationResponse(notification, textResponse.ActionIdentifier, textResponse.UserText);
            //        await this.sdelegate.Value.OnEntry(shinyResponse);
            //    });
            //}
            //else
            //{
            //    await Log.SafeExecute(async () =>
            //    {
            //        var shinyResponse = new NotificationResponse(notification, response.ActionIdentifier, null);
            //        await this.sdelegate.Value.OnEntry(shinyResponse);
            //    });
            //}
            completionHandler();
        }


        public override async void WillPresentNotification(UNUserNotificationCenter center, UNNotification notification, Action<UNNotificationPresentationOptions> completionHandler)
        {
            //await Log.SafeExecute(async () =>
            //{
            //    var shinyNotification = notification.Request.FromNative();
            //    await this.sdelegate.Value.OnReceived(shinyNotification);
            //});
            completionHandler(UNNotificationPresentationOptions.Alert);
        }
    }
}
