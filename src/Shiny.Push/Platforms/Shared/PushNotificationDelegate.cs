using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Shiny.Notifications;


namespace Shiny.Push
{
    public class PushNotificationDelegate : INotificationDelegate
    {
        readonly IEnumerable<IPushDelegate> delegates;
        readonly IMessageBus messageBus;


        public PushNotificationDelegate(IEnumerable<IPushDelegate> delegates, IMessageBus messageBus)
        {
            this.delegates = delegates;
            this.messageBus = messageBus;
        }


#if __ANDROID__
        public Task OnEntry(NotificationResponse response) => this
            .delegates?
            .RunDelegates(x => x.OnEntry(new PushEntryArgs(
                response.Notification.Channel,
                response.ActionIdentifier,
                response.Text,
                response.Notification.Payload
            ))) ?? Task.CompletedTask;
#else
        public Task OnEntry(NotificationResponse response) => Task.CompletedTask;
#endif

#if __IOS__
        public async Task OnReceived(Notification notification)
        {
            await this.delegates
                .RunDelegates(x => x.OnReceived(notification.Payload))
                .ConfigureAwait(false);

            this.messageBus.Publish(nameof(PushNotificationDelegate), notification.Payload);
        }
#else
        public Task OnReceived(Notification notification) => Task.CompletedTask;
#endif
    }
}
