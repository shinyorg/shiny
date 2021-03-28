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


        public Task OnEntry(NotificationResponse response) => this
            .delegates?
            .RunDelegates(x => x.OnEntry(new PushNotificationResponse(
                response.Notification,
                response.ActionIdentifier,
                response.Text
            ))) ?? Task.CompletedTask;
    }
}
