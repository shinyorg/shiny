using System.Collections.Generic;

namespace Shiny.Notifications;


/// <summary>
/// Manages the persisted registry of notification channels independent of the notification manager.
/// </summary>
public interface IChannelManager
{
    /// <summary>
    /// Registers a new notification channel.
    /// </summary>
    /// <param name="channel">The channel definition to add.</param>
    void Add(Channel channel);


    /// <summary>
    /// Removes a specific channel by identifier. Any pending notifications referencing this channel will have their channel reference cleared.
    /// </summary>
    /// <param name="channelId">The identifier of the channel to remove.</param>
    void Remove(string channelId);


    /// <summary>
    /// Removes all registered channels and clears the channel reference from any pending notifications.
    /// </summary>
    void Clear();


    /// <summary>
    /// Retrieves a channel by its identifier.
    /// </summary>
    /// <param name="channelId">The identifier of the channel.</param>
    /// <returns>The matching channel, or null if no channel with that identifier exists.</returns>
    Channel? Get(string channelId);


    /// <summary>
    /// Returns the list of all currently registered notification channels.
    /// </summary>
    /// <returns>A list of registered channels.</returns>
    IList<Channel> GetAll();
}
