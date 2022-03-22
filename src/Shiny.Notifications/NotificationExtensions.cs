using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Shiny.Infrastructure;


namespace Shiny.Notifications
{
    public static class NotificationExtensions
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
            { 
                foreach (var action in channel.Actions)
                    action.AssertValid();
            }
        }


        public static void AssertValid(this Notification notification)
        {
            var triggers = 0;
            triggers += notification.ScheduleDate == null ? 0 : 1;
            triggers += notification.Geofence == null ? 0 : 1;
            triggers += notification.RepeatInterval == null ? 0 : 1;
            if (triggers > 1)
                throw new InvalidOperationException("You cannot mix scheduled date, repeated interval, and/or geofences on a notification");

            if (notification.Message.IsEmpty())
                throw new InvalidOperationException("You must have a message on your notification");

            if (notification.BadgeCount < 0)
                throw new InvalidOperationException("BadgeCount must be >= 0");

            if (notification.ScheduleDate != null && notification.ScheduleDate < DateTimeOffset.UtcNow)
                throw new InvalidOperationException("ScheduleDate must be set in the future");

            notification.RepeatInterval?.AssertValid();
            notification.Geofence?.AssertValid();
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
            var notifications = await repository.GetNotificationsByChannel(channelId);

            foreach (var notification in notifications)
            {
                notification.Channel = null;
                await repository.Set(notification.Id.ToString(), notification);
            }
        }


        internal static async Task RemoveAllChannels(this IRepository repository)
        {
            await repository.Clear<Channel>();
            var notifications = await repository.GetList<Notification>(x => !x.Channel.IsEmpty());

            foreach (var notification in notifications)
            {
                notification.Channel = null;
                await repository.Set(notification.Id.ToString(), notification);
            }
        }

        internal static Task<Channel?> GetChannel(this IRepository repository, string channelIdentifier)
            => repository.Get<Channel>(channelIdentifier);

        internal static Task<IList<Channel>> GetChannels(this IRepository repository)
            => repository.GetList<Channel>();


        internal static async Task RemoveChannelFromPendingNotificationsByChannel(this IRepository repository, string channelId)
        {
            var notifications = await repository.GetNotificationsByChannel(channelId);
            foreach (var notification in notifications)
            {
                notification.Channel = null;
                await repository.Set(notification.Id.ToString(), notification);
            }
        }


        internal static Task<IList<Notification>> GetNotificationsByChannel(this IRepository repository, string channelId)
            => repository.GetList<Notification>(x =>
                 x.Channel != null && x.Channel.Equals(channelId, StringComparison.InvariantCultureIgnoreCase)
            );
    }
}
