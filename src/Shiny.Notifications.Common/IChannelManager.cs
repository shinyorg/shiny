using System.Collections.Generic;
using System.Threading.Tasks;


namespace Shiny.Notifications
{
    public interface IChannelManager
    {
        /// <summary>
        /// Add a new channel
        /// </summary>
        /// <param name="channel"></param>
        /// <returns></returns>
        Task Add(Channel channel);


        /// <summary>
        /// Remove a specific channel - any pending notifications on this channel will have the channel removed from them
        /// </summary>
        /// <returns></returns>
        Task Remove(string channelId);


        /// <summary>
        /// Removes all channels - this will remove all channels from all notifications
        /// </summary>
        /// <returns></returns>
        Task Clear();


        /// <summary>
        /// 
        /// </summary>
        /// <param name="channelId"></param>
        /// <returns></returns>
        Task<Channel?> Get(string channelId);


        /// <summary>
        /// Gets list of channels
        /// </summary>
        /// <returns></returns>
        Task<IList<Channel>> GetAll();
    }
}
