using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Shiny.Infrastructure;


namespace Shiny.Notifications
{
    public static class Extensions
    {
        /// <summary>
        /// This will overwrite all current channels - any channels that no longer exist will be blanked out on any pending local notifications
        /// </summary>
        /// <param name="channels"></param>
        /// <returns></returns>
        public static async Task SetChannels(this INotificationManager manager, params Channel[] channels)
        {
            var currentChannels = await manager.GetChannels();

            // if the current channel doesn't exist in the incoming channels, remove it and nullify channel ID from pending notifications
            foreach (var currentChannel in currentChannels)
            {
                // if the currentchannel doesn't exist in incoming channels, remove the channel
                var exists = channels.Any(x => x.Identifier.Equals(
                    currentChannel.Identifier, StringComparison.InvariantCultureIgnoreCase
                ));
                if (!exists)
                    await manager.RemoveChannel(currentChannel.Identifier);
            }

            // if the incoming channels don't exist in the current channels, create them
            foreach (var channel in channels)
            {
                var exists = currentChannels.Any(x => x.Identifier.Equals(
                    channel.Identifier, StringComparison.InvariantCultureIgnoreCase
                ));
                if (!exists)
                    await manager.AddChannel(channel);
            }
        }


        public static void AssertValid(this Channel channel)
        {
            if (channel.Identifier.IsEmpty())
                throw new ArgumentException("Channel identifier is required", nameof(channel.Identifier));

            if (channel.Actions != null)
                foreach (var action in channel.Actions)
                    action.AssertValid();
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


        public static Task Send(this INotificationManager notifications, string title, string message, string? channel = null, DateTime? scheduleDate = null)
            => notifications.Send(new Notification
            {
                Title = title,
                Message = message,
                Channel = channel,
                ScheduleDate = scheduleDate
            });


        internal static Task SetChannel(this IRepository repository, Channel channel)
            => repository.Set(channel.Identifier, channel);


        internal static async Task RemoveChannel(this IRepository repository, string channelId)
        {
            await repository.Remove<Channel>(channelId);

            var notifications = await repository.GetAll<Notification>();
            foreach (var notification in notifications)
            {
                if (notification.Channel?.Equals(channelId, StringComparison.InvariantCultureIgnoreCase) ?? false)
                {
                    notification.Channel = null;
                    await repository.Set(notification.Id.ToString(), notification);
                }
            }
        }


        internal static async Task RemoveAllChannels(this IRepository repository)
        {
            await repository.Clear<Channel>();

            var notifications = await repository.GetAll<Notification>();
            foreach (var notification in notifications)
            {
                if (!notification.Channel.IsEmpty())
                {
                    notification.Channel = null;
                    await repository.Set(notification.Id.ToString(), notification);
                }
            }
        }

        internal static Task<Channel?> GetChannel(this IRepository repository, string channelIdentifier)
            => repository.Get<Channel>(channelIdentifier);

        internal static Task<IList<Channel>> GetChannels(this IRepository repository)
            => repository.GetAll<Channel>();


        internal static async Task RemoveChannelFromPendingNotificationsByChannel(this IRepository repository, string channelId)
        {
            var notifications = await repository.GetAll<Notification>();
            foreach (var notification in notifications)
            {
                if (notification.Channel?.Equals(channelId, StringComparison.InvariantCultureIgnoreCase) ?? false)
                {
                    notification.Channel = null;
                    await repository.Set(notification.Id.ToString(), notification);
                }
            }
        }
    }
}
