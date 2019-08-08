using System;
using System.Threading.Tasks;
using Shiny.Infrastructure;
using Shiny.Logging;


namespace Shiny.Notifications
{
    public class NotificationProcessor
    {
        readonly INotificationDelegate ndelegate;
        readonly IRepository repository;


        public NotificationProcessor(INotificationDelegate ndelegate, IRepository repository)
        {
            this.ndelegate = ndelegate;
            this.repository = repository;
        }


        public Task Entry(string notificationId)
            => this.Execute(notificationId, not => this.ndelegate.OnEntry(not));


        public Task Receive(string notificationId)
            => this.Execute(notificationId, not => this.ndelegate.OnReceived(not));


        async Task Execute(string notificationId, Func<Notification, Task> execute)
        {
            try
            {
                var not = await this.repository.Get<Notification>(notificationId);
                if (not != null)
                    await execute.Invoke(not);
            }
            catch (Exception ex)
            {
                Log.Write(ex);
            }
        }
    }
}
