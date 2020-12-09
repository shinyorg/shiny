using System;
using System.Collections.Generic;
using System.Threading.Tasks;


namespace Shiny.Notifications
{
    public interface INotificationManager
    {
        /// <summary>
        /// Create channel
        /// </summary>
        /// <param name="channel"></param>
        Task CreateChannel(Channel channel);


        /// <summary>
        /// Delete channel
        /// </summary>
        /// <param name="identifier"></param>
        Task DeleteChannel(string identifier);


        /// <summary>
        /// Gets list of channels
        /// </summary>
        /// <returns></returns>
        Task<IList<Channel>> GetChannels();

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
        /// Gets all pending notifications
        /// </summary>
        /// <returns></returns>
        Task<IEnumerable<Notification>> GetPending();


        /// <summary>
        /// Cancels a specified notification
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        Task Cancel(int id);


        /// <summary>
        /// Send a notification
        /// </summary>
        /// <param name="notification"></param>
        /// <returns>The messageID that you can use to cancel with</returns>
        Task Send(Notification notification);


        /// <summary>
        /// Sets the app icon badge
        /// </summary>
        int Badge { get; set; }
    }
}