using System;
using System.Threading.Tasks;
using Shiny.Notifications;


namespace Shiny.Testing.Notifications
{
    public class TestNotifications : INotificationManager
    {
        public AccessState RequestAccessReply { get; set; } = AccessState.Available;
        public Notification LastNotification { get; private set; }


        public Task Clear() => Task.CompletedTask;
        public Task<AccessState> RequestAccess() => Task.FromResult(this.RequestAccessReply);
        public Task Send(Notification notification)
        {
            this.LastNotification = notification;
            return Task.CompletedTask;
        }
    }
}
