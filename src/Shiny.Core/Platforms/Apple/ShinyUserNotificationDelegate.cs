using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using UserNotifications;


namespace Shiny
{
    public class ShinyUserNotificationDelegate : UNUserNotificationCenterDelegate
    {
        readonly ILogger logger = ShinyHost.LoggerFactory.CreateLogger(nameof(ShinyUserNotificationDelegate));


        readonly List<Func<UNNotificationResponse, Task>> receiveNotifications = new List<Func<UNNotificationResponse, Task>>();
        public IDisposable RegisterForNotificationReceived(Func<UNNotificationResponse, Task> task)
        {
            this.receiveNotifications.Add(task);
            return Disposable.Create(() => this.receiveNotifications.Remove(task));
        }


        readonly List<Func<UNNotification, Task>> presentNotifications = new List<Func<UNNotification, Task>>();
        public IDisposable RegisterForNotificationPresentation(Func<UNNotification, Task> task)
        {
            this.presentNotifications.Add(task);
            return Disposable.Create(() => this.presentNotifications.Remove(task));
        }


        public override async void DidReceiveNotificationResponse(UNUserNotificationCenter center, UNNotificationResponse response, Action completionHandler)
        {
            var events = this.receiveNotifications.ToList();

            foreach (var receive in events)
            {
                try
                {
                    await receive.Invoke(response);
                }
                catch (Exception ex)
                {
                    this.logger.LogError(ex, "DidReceiveNotificationResponse");
                }
            }
            completionHandler();
        }


        public override async void WillPresentNotification(UNUserNotificationCenter center, UNNotification notification, Action<UNNotificationPresentationOptions> completionHandler)
        {
            var events = this.presentNotifications.ToList();

            foreach (var present in events)
            {
                try
                {
                    await present.Invoke(notification);
                }
                catch (Exception ex)
                {
                    this.logger.LogError(ex, "WillPresentNotification");
                }
            }
            completionHandler.Invoke(UNNotificationPresentationOptions.Alert);
        }
    }
}
