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
        ///
        /// </summary>
        /// <param name="notification"></param>
        Task OnEntry(Notification notification);
    }
}
