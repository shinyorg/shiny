using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Shiny.Notifications
{
    public static class ChannelExtensions
    {
        /// <summary>
        /// This will overwrite all current channels - any channels that no longer exist will be blanked out on any pending local notifications
        /// </summary>
        /// <param name="channels"></param>
        /// <returns></returns>
        public static async Task SetChannels(this IChannelManager manager, params Channel[] channels)
        {
            var currentChannels = await manager.GetAll();

            // if the current channel doesn't exist in the incoming channels, remove it and nullify channel ID from pending notifications
            foreach (var currentChannel in currentChannels)
            {
                // if the currentchannel doesn't exist in incoming channels, remove the channel
                var exists = channels.Any(x => x.Identifier.Equals(
                    currentChannel.Identifier, StringComparison.InvariantCultureIgnoreCase
                ));
                if (!exists)
                    await manager.Remove(currentChannel.Identifier);
            }

            // if the incoming channels don't exist in the current channels, create them
            foreach (var channel in channels)
            {
                var exists = currentChannels.Any(x => x.Identifier.Equals(
                    channel.Identifier, StringComparison.InvariantCultureIgnoreCase
                ));
                if (!exists)
                    await manager.Remove(channel.Identifier);
            }
        }


        public static void AssertValid(this Channel channel)
        {
            if (channel.Identifier.IsEmpty())
                throw new ArgumentException("Channel identifier is required", nameof(channel.Identifier));

            if (channel.Actions != null)
            {
                foreach (var action in channel.Actions)
                    action.AssertValid();
            }
        }


        public static void SetSoundFromEmbeddedResource(this Channel channel, Assembly assembly, string resourceName)
            => channel.CustomSoundPath = ShinyHost
                .Resolve<IPlatform>()
                .ResourceToFilePath(assembly, resourceName);


        public static void AssertValid(this ChannelAction action)
        {
            if (action.Identifier.IsEmpty())
                throw new ArgumentException("ChannelAction Identifier is required", nameof(action.Identifier));

            if (action.Title.IsEmpty())
                throw new ArgumentException("ChannelAction Title is required", nameof(action.Title));
        }
    }
}
