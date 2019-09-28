using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Shiny.Notifications;


namespace Shiny.Testing.Notifications
{
    public class TestNotifications : INotificationManager
    {
        public int CurrentNotificationId { get; set; } = 1;
        public int CurrentBadge { get; set; }
        public AccessState RequestAccessReply { get; set; } = AccessState.Available;
        public Notification LastNotification { get; private set; }

        public Task Cancel(int id) => Task.CompletedTask;
        public Task Clear() => Task.CompletedTask;

        public Task<int> GetBadge() => Task.FromResult(this.CurrentBadge);
        public Task SetBadge(int value)
        {
            this.CurrentBadge = value;
            return Task.CompletedTask;
        }
        public Task<IEnumerable<Notification>> GetPending() => Task.FromResult(Enumerable.Empty<Notification>());

        public Task<AccessState> RequestAccess() => Task.FromResult(this.RequestAccessReply);
        public Task Send(Notification notification)
        {
            if (notification.Id == 0)
                notification.Id = this.CurrentNotificationId++;

            this.LastNotification = notification;
            return Task.CompletedTask;
        }


    }
}
