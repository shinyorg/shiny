using System;
using System.Threading.Tasks;
using Shiny.Notifications;


namespace Shiny.Push
{
    public class PushNotificationDelegate : INotificationDelegate
    {
        readonly IPushDelegate pushDelegate;
        readonly IMessageBus messageBus;


        public PushNotificationDelegate(IPushDelegate pushDelegate, IMessageBus messageBus)
        {
            this.pushDelegate = pushDelegate;
            this.messageBus = messageBus;
        }


        public Task OnEntry(NotificationResponse response) => this
            .pushDelegate
            .OnEntry(new PushEntryArgs(
                response.Notification.Category,
                response.ActionIdentifier,
                response.Text,
                response.Notification.Payload
            ));


#if __IOS__
        public async Task OnReceived(Notification notification)
        {
            await this.pushDelegate.OnReceived(notification.Payload);
            this.messageBus.Publish(nameof(PushNotificationDelegate), notification.Payload);
        }
#else
        public Task OnReceived(Notification notification) => Task.CompletedTask;
#endif
    }
}
