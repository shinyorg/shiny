using System;
using System.Collections.Generic;
using System.Linq;
using Windows.ApplicationModel.Background;
using Windows.Networking.PushNotifications;
using Windows.UI.Notifications;


namespace Shiny.Push
{
    public class PushNotificationBackgroundTaskProcessor : IBackgroundTaskProcessor
    {
        readonly IEnumerable<IPushDelegate> delegates;


        public PushNotificationBackgroundTaskProcessor(IEnumerable<IPushDelegate> delegates)
            => this.delegates = delegates;


        public async void Process(IBackgroundTaskInstance taskInstance)
        {
            // TODO: resolve native adapter
            var deferral = taskInstance.GetDeferral();
            //var notification = new Notification();
            IDictionary<string, string>? dict = null;
            var fire = true;

            if (taskInstance.TriggerDetails is RawNotification raw)
            {
                dict = raw.Headers?.ToDictionary(x => x.Key, x => x.Value);
            }
            else if (taskInstance.TriggerDetails is ToastNotification toast)
            {
                dict = toast.Data?.Values?.ToDictionary(x => x.Key, x => x.Value);
            }
            else if (taskInstance.TriggerDetails is TileNotification)
            {
            }
            else
            {
                fire = false;
            }

            if (fire)
            {
                var response = new PushNotification(dict ?? new Dictionary<string, string>(0), null);
                await this.delegates
                    .RunDelegates(x => x.OnEntry(response))
                    .ConfigureAwait(false);
            }
            deferral.Complete();
        }
    }
}
