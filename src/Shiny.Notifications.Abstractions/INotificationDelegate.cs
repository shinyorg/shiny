using System;
using System.Threading.Tasks;


namespace Shiny.Notifications
{
    public interface INotificationDelegate
    {
        /// <summary>
        /// This event will fire when the notification
        /// </summary>
        /// <param name="notification"></param>
        Task OnReceived(Notification notification);

        /// <summary>
        /// This will fire when the user taps on a notification (or responds using a command)
        /// </summary>
        /// <param name="response"></param>
        Task OnEntry(NotificationResponse response);
    }
}
