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