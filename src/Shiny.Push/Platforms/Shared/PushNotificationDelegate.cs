using System;
using System.Threading.Tasks;
using Shiny.Notifications;


namespace Shiny.Push
{
    public class PushNotificationDelegate : INotificationDelegate
    {
        readonly IPushDelegate pushDelegate;
        public PushNotificationDelegate(IPushDelegate pushDelegate)
            => this.pushDelegate = pushDelegate;


        public Task OnEntry(NotificationResponse response) => this
            .pushDelegate
            .OnEntry(new PushEntryArgs(
                response.Notification.Category,
                response.ActionIdentifier,
                response.Text,
                response.Notification.Payload
            ));
       

        public Task OnReceived(Notification notification) => Task.CompletedTask;
    }
}
