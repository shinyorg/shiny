using System;
using System.Collections.Generic;
using System.Linq;
using Windows.ApplicationModel.Background;
using Windows.Networking.PushNotifications;
using Windows.UI.Notifications;
using Notification = Shiny.Notifications.Notification;


namespace Shiny.Push
{
    public class PushNotificationBackgroundTaskProcessor : IBackgroundTaskProcessor
    {
        readonly IEnumerable<IPushDelegate> delegates;


        public PushNotificationBackgroundTaskProcessor(IEnumerable<IPushDelegate> delegates)
            => this.delegates = delegates;


        public async void Process(IBackgroundTaskInstance taskInstance)
        {
            var deferral = taskInstance.GetDeferral();
            var notification = new Notification();
            var fire = true;

            if (taskInstance.TriggerDetails is RawNotification raw)
            {
                notification.Payload = raw.Headers?.ToDictionary(x => x.Key, x => x.Value);
                notification.Channel = raw.ChannelId;
                notification.Message = raw.Content;
            }
            else if (taskInstance.TriggerDetails is ToastNotification toast)
            {
                notification.Payload = toast.Data?.Values?.ToDictionary(x => x.Key, x => x.Value);
            }
            else if (taskInstance.TriggerDetails is TileNotification tile)
            {
            }
            else
            {
                fire = false;
            }

            if (fire)
            {
                var response = new PushNotificationResponse(notification, null, null);
                await this.delegates
                    .RunDelegates(x => x.OnEntry(response))
                    .ConfigureAwait(false);
            }
            deferral.Complete();
        }
    }
}
