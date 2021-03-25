using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Shiny.Notifications;


namespace Shiny.Push
{
    public class PushNotificationDelegate : INotificationDelegate
    {
        static readonly IDictionary<string, string> EmptyDictionary = new Dictionary<string, string>(0);

        readonly IEnumerable<IPushDelegate> delegates;
        public PushNotificationDelegate(IEnumerable<IPushDelegate> delegates) => this.delegates = delegates;


        public Task OnEntry(NotificationResponse response) => this
            .delegates?
            .RunDelegates(x => x.OnAction(
                response.Notification?.Payload ?? EmptyDictionary,
                response.Notification,
                true
            )) ?? Task.CompletedTask;
    }
}
