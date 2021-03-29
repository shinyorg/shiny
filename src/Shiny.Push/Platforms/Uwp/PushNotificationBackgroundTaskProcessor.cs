using System;
using Windows.ApplicationModel.Background;
using Windows.Networking.PushNotifications;
using Windows.UI.Notifications;


namespace Shiny.Push
{
    public class PushNotificationBackgroundTaskProcessor : IBackgroundTaskProcessor
    {
        readonly IServiceProvider serviceProvider;


        public PushNotificationBackgroundTaskProcessor(IServiceProvider serviceProvider)
            => this.serviceProvider = serviceProvider;


        public async void Process(IBackgroundTaskInstance taskInstance)
        {
            var deferral = taskInstance.GetDeferral();
            var headers = PushManager.ExtractHeaders(taskInstance.TriggerDetails);

            var fire = taskInstance.TriggerDetails is RawNotification ||
                       taskInstance.TriggerDetails is ToastNotification ||
                       taskInstance.TriggerDetails is TileNotification;

            // could translate one of those to the notification?
            if (fire)
            {
                var notification = new Shiny.Notifications.Notification
                {
                    Title = "",
                    Payload = headers
                };
                var response = new PushNotificationResponse(notification, null, null);
                await this.serviceProvider.RunDelegates<IPushDelegate>(x => x.OnEntry(response));
            }
            deferral.Complete();
        }
    }
}
