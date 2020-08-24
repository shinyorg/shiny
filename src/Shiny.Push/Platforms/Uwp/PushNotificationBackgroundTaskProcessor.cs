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
        readonly IServiceProvider serviceProvider;


        public PushNotificationBackgroundTaskProcessor(IServiceProvider serviceProvider)
            => this.serviceProvider = serviceProvider;


        public async void Process(IBackgroundTaskInstance taskInstance)
        {
            // TODO: push delegate OnEntry?
            var deferral = taskInstance.GetDeferral();
            var fire = true;
            IDictionary<string, string> headers = new Dictionary<string, string>();

            if (taskInstance.TriggerDetails is RawNotification raw)
            {
                if (raw.Headers != null)
                    headers = raw.Headers.ToDictionary(x => x.Key, x => x.Value);
            }
            else if (taskInstance.TriggerDetails is ToastNotification toast)
            {
                if (toast.Data?.Values != null)
                    headers = toast.Data.Values;
            }
            else if (taskInstance.TriggerDetails is TileNotification tile)
            {
                headers.Add("Tag", tile.Tag);
            }
            else
            {
                fire = false;
            }

            if (fire)
                await this.serviceProvider.RunDelegates<IPushDelegate>(x => x.OnReceived(headers));

            deferral.Complete();
        }
    }
}
