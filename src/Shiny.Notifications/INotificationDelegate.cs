using System;
using System.Threading.Tasks;
using Shiny.Infrastructure;


namespace Shiny.Notifications
{
    public interface INotificationDelegate : IShinyDelegate
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
