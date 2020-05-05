using System;
using System.Collections.Generic;
using System.Linq;
using Windows.ApplicationModel.Background;
using Windows.Networking.PushNotifications;


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
            var notification = taskInstance.TriggerDetails as RawNotification;

            if (notification != null)
            {
                var headers = notification
                    .Headers?
                    .ToDictionary(
                        x => x.Key,
                        x => x.Value
                    ) ?? new Dictionary<string, string>(0);

                await this.serviceProvider.RunDelegates<IPushDelegate>(x => x.OnReceived(headers));
            }

            deferral.Complete();
        }
    }
}
