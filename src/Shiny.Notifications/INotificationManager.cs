using System.Collections.Generic;
using System.Threading.Tasks;

namespace Shiny.Notifications;


/// <summary>
/// Cross-platform manager for sending, scheduling, and cancelling local notifications, plus managing notification channels and permissions.
/// </summary>
public interface INotificationManager
{
    /// <summary>
    /// Registers a new notification channel. Channels group notifications and define their importance, sound, and available actions.
    /// </summary>
    /// <param name="channel">The channel definition to add.</param>
    void AddChannel(Channel channel);


    /// <summary>
    /// Removes a specific channel by its identifier. Any pending notifications targeting this channel are also removed.
    /// </summary>
    /// <param name="channelId">The identifier of the channel to remove.</param>
    void RemoveChannel(string channelId);


    /// <summary>
    /// Removes all registered channels and any pending notifications associated with them.
    /// </summary>
    void ClearChannels();


    /// <summary>
    /// Retrieves a previously registered channel by its identifier.
    /// </summary>
    /// <param name="channelId">The identifier of the channel.</param>
    /// <returns>The matching channel, or null if no channel with that identifier exists.</returns>
    Channel? GetChannel(string channelId);


    /// <summary>
    /// Returns the list of all currently registered notification channels.
    /// </summary>
    /// <returns>A list of registered channels.</returns>
    IList<Channel> GetChannels();


    /// <summary>
    /// Returns the current notification access state without prompting the user.
    /// </summary>
    /// <param name="flags">The capabilities to check (e.g. TimeSensitivity, LocationAware).</param>
    /// <returns>The current platform access state for the requested capabilities.</returns>
    Task<AccessState> GetCurrentAccess(AccessRequestFlags flags = AccessRequestFlags.Notification);

    /// <summary>
    /// Requests the required platform permissions, prompting the user if necessary.
    /// </summary>
    /// <param name="flags">The capabilities to request (e.g. TimeSensitivity, LocationAware).</param>
    /// <returns>The resulting access state after the user responds.</returns>
    Task<AccessState> RequestAccess(AccessRequestFlags flags = AccessRequestFlags.Notification);


    /// <summary>
    /// Retrieves a single pending notification by its identifier.
    /// </summary>
    /// <param name="notificationId">The notification identifier.</param>
    /// <returns>The notification, or null if no pending notification matches.</returns>
    Task<Notification>? GetNotification(int notificationId);


    /// <summary>
    /// Cancels a single notification (displayed or pending) by its identifier.
    /// </summary>
    /// <param name="id">The notification identifier.</param>
    Task Cancel(int id);


    /// <summary>
    /// Cancels notifications matching the specified scope.
    /// </summary>
    /// <param name="cancelScope">DisplayedOnly clears only notifications currently on screen; Pending clears scheduled, interval, and geofence-triggered notifications; All clears both.</param>
    Task Cancel(CancelScope cancelScope = CancelScope.All);


    /// <summary>
    /// Returns all notifications currently scheduled, repeating, or awaiting a geofence trigger.
    /// </summary>
    /// <returns>The list of pending notifications.</returns>
    Task<IList<Notification>> GetPendingNotifications();


    /// <summary>
    /// Sends a notification immediately, or schedules it if a trigger (schedule date, interval, geofence) is set.
    /// </summary>
    /// <param name="notification">The notification to send.</param>
    Task Send(Notification notification);
}
