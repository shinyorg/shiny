using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Shiny.Notifications;


namespace Shiny.Testing.Notifications
{
    public class TestNotificationManager : INotificationManager
    {
        public int CurrentNotificationId { get; set; } = 1;
        public int CurrentBadge { get; set; }
        public AccessState RequestAccessReply { get; set; } = AccessState.Available;
        public Notification LastNotification { get; private set; }

        public Task Cancel(int id) => Task.CompletedTask;
        public Task Clear() => Task.CompletedTask;
        public int Badge { get; set; }
        public Task<IEnumerable<Notification>> GetPending() => Task.FromResult(Enumerable.Empty<Notification>());

        public Task<AccessState> RequestAccess() => Task.FromResult(this.RequestAccessReply);
        public Task Send(Notification notification)
        {
            if (notification.Id == 0)
                notification.Id = this.CurrentNotificationId++;

            this.LastNotification = notification;
            return Task.CompletedTask;
        }


        public List<NotificationCategory> RegisteredCategories { get; } = new List<NotificationCategory>();
        public void RegisterCategory(NotificationCategory category)
        {
            this.RegisteredCategories.Add(category);
        }
    }
}
