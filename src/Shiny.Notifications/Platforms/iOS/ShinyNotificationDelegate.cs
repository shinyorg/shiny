using System;
using System.Threading.Tasks;
using Shiny.Logging;
using UserNotifications;


namespace Shiny.Notifications
{
    public class ShinyNotificationDelegate : UNUserNotificationCenterDelegate
    {
        public override async void DidReceiveNotificationResponse(UNUserNotificationCenter center, UNNotificationResponse response, Action completionHandler)
        {
            await this.Execute(response.Notification.Request, (not, del) => del.OnEntry(not));
            completionHandler();
        }


        public override async void WillPresentNotification(UNUserNotificationCenter center, UNNotification notification, Action<UNNotificationPresentationOptions> completionHandler)
        {
            await this.Execute(notification.Request, (not, del) => del.OnReceived(not));
            completionHandler(UNNotificationPresentationOptions.Alert);
        }


        async Task Execute(UNNotificationRequest request, Func<Notification, INotificationDelegate, Task> execute)
        {
            try
            {
                var not = request.FromNative();
                var del = ShinyHost.Resolve<INotificationDelegate>();
                await execute(not, del);
            }
            catch (Exception ex)
            {
                Log.Write(ex);
            }
        }
    }
}
