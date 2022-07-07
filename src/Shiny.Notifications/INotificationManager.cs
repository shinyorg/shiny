using System;
using System.Collections.Generic;
using System.Threading.Tasks;


namespace Shiny.Notifications
{
    public interface INotificationManager
    {
        /// <summary>
        /// Add a new channel
        /// </summary>
        /// <param name="channel"></param>
        /// <returns></returns>
        Task AddChannel(Channel channel);


        /// <summary>
        /// Remove a specific channel - any pending notifications on this channel will also be removed
        /// </summary>
        /// <returns></returns>
        Task RemoveChannel(string channelId);


        /// <summary>
        /// Removes all channels and any pending notifications
        /// </summary>
        /// <returns></returns>
        Task ClearChannels();


        /// <summary>
        /// Gets list of channels
        /// </summary>
        /// <returns></returns>
        Task<IList<Channel>> GetChannels();


        /// <summary>
        /// Get a specific channel by ID
        /// </summary>
        /// <param name="channelId"></param>
        /// <returns></returns>
        Task<Channel?> GetChannel(string channelId);

        /// <summary>
        /// Requests/ensures appropriate platform permissions where necessary
        /// </summary>
        /// <param name="flags">You can request multiple types of permissions (TimeSensitive, LocationAware)</param>
        /// <returns></returns>
        Task<AccessState> RequestAccess(AccessRequestFlags flags = AccessRequestFlags.Notification);


        /// <summary>
        /// Get a notification by id
        /// </summary>
        /// <param name="notificationId"></param>
        /// <returns>null if not found</returns>
        Task<Notification?> GetNotification(int notificationId);


        /// <summary>
        /// Cancels a specified notification
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        Task Cancel(int id);


        /// <summary>
        /// Cancels notifications
        /// </summary>
        /// <param name="cancelScope">DisplayedOnly - clears only notifications that are on the home screen.  Pending - anything that has a trigger (geofence, schedule, interval).  All - the default and does everything</param>
        /// <returns></returns>
        Task Cancel(CancelScope cancelScope = CancelScope.All);


        /// <summary>
        /// Gets all pending notifications
        /// </summary>
        /// <returns></returns>
        Task<IEnumerable<Notification>> GetPendingNotifications();


        /// <summary>
        /// Send a notification
        /// </summary>
        /// <param name="notification"></param>
        /// <returns>The messageID that you can use to cancel with</returns>
        Task Send(Notification notification);


        /// <summary>
        /// Get the app icon badge
        /// </summary>
        Task<int> GetBadge();


        /// <summary>
        /// Set the badge number
        /// </summary>
        /// <param name="badge">0 or null to clear</param>
        /// <returns></returns>
        Task SetBadge(int? badge);
    }
}