using System;
using System.Threading.Tasks;
using Shiny.Notifications;

namespace Shiny.Push
{
    public class ShinyNotificationDelegate : INotificationDelegate
    {

        public Task OnEntry(NotificationResponse response)
        {
            return Task.CompletedTask;
        }


        public Task OnReceived(Notification notification)
        {
            return Task.CompletedTask;
        }
    }
}
