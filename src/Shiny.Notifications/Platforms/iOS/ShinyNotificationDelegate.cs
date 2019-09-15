using System;
using System.Threading.Tasks;
using Shiny.Logging;
using UserNotifications;


namespace Shiny.Notifications
{
    public class ShinyNotificationDelegate : UNUserNotificationCenterDelegate
    {
        readonly Lazy<INotificationDelegate> sdelegate = new Lazy<INotificationDelegate>(() => ShinyHost.Resolve<INotificationDelegate>());


        public override async void DidReceiveNotificationResponse(UNUserNotificationCenter center, UNNotificationResponse response, Action completionHandler)
        {
            await this.Execute(response.Notification.Request, x => this.sdelegate.Value?.OnReceived(x));
            completionHandler();
        }


        public override async void WillPresentNotification(UNUserNotificationCenter center, UNNotification notification, Action<UNNotificationPresentationOptions> completionHandler)
        {
            await this.Execute(notification.Request, x => this.sdelegate.Value?.OnEntry(x));
            completionHandler(UNNotificationPresentationOptions.Alert);
        }


        async Task Execute(UNNotificationRequest request, Func<Notification, Task> execute)
        {
            try
            {
                if (this.sdelegate.Value == null)
                    return;

                var not = request.FromNative();
                await execute(not);
            }
            catch (Exception ex)
            {
                Log.Write(ex);
            }
        }
    }
}
