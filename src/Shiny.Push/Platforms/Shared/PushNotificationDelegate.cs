using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Shiny.Notifications;


namespace Shiny.Push
{
    public class PushNotificationDelegate : INotificationDelegate
    {
        readonly IEnumerable<IPushDelegate> delegates;
        public PushNotificationDelegate(IEnumerable<IPushDelegate> delegates) => this.delegates = delegates;


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
    }
}
