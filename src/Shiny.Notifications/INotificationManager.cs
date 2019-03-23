using System;
using System.Collections.Generic;
using System.Threading.Tasks;


namespace Shiny.Notifications
{
    public interface INotificationManager
    {
        /// <summary>
        /// Requests/ensures appropriate platform permissions where necessary
        /// </summary>
        /// <returns></returns>
        Task<AccessState> RequestAccess();


        /// <summary>
        /// Clears all notifications
        /// </summary>
        /// <returns></returns>
        Task Clear();


        /// <summary>
        /// Send a notification
        /// </summary>
        /// <param name="notification"></param>
        /// <returns>The messageID that you can use to cancel with</returns>
        Task Send(Notification notification);
    }
}