using System;
using System.Threading.Tasks;


namespace Shiny.Notifications
{
    public interface INotificationDelegate
    {
        /// <summary>
        /// This will fire when the user taps on a notification (or responds using a command)
        /// </summary>
        /// <param name="response"></param>
        Task OnEntry(NotificationResponse response);
    }
}
