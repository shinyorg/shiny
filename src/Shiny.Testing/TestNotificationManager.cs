using System;
using System.Threading.Tasks;
using Shiny.Notifications;


namespace Shiny.Testing
{
    public class TestNotifications : INotificationManager
    {
        public AccessState ReturnState { get; set; } = AccessState.Available;
        public Notification LastNotification { get; private set; }


        public Task Clear() => Task.CompletedTask;
        public Task<AccessState> RequestAccess() => Task.FromResult(this.ReturnState);
        public Task Send(Notification notification)
        {
            this.LastNotification = notification;
            return Task.CompletedTask;
        }
    }
}
