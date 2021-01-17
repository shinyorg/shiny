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
            // TODO: push delegate OnEntry?
            var deferral = taskInstance.GetDeferral();
            var headers = PushManager.ExtractHeaders(taskInstance.TriggerDetails);

            var fire = taskInstance.TriggerDetails is RawNotification ||
                       taskInstance.TriggerDetails is ToastNotification ||
                       taskInstance.TriggerDetails is TileNotification;
            if (fire)
                await this.serviceProvider.RunDelegates<IPushDelegate>(x => x.OnReceived(headers));

            deferral.Complete();
        }
    }
}
