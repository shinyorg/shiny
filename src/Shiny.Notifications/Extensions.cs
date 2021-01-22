using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Shiny.Infrastructure;


namespace Shiny.Notifications
{
    public static class Extensions
    {
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


        public static async Task CreateChannel(this INotificationManager manager, Channel channel)
        {
            var channels = await manager.GetChannels();
            var newChannels = channels.ToList();
            newChannels.Add(channel);
            await manager.SetChannels(newChannels.ToArray());
        }


        public static async Task DeleteChannel(this INotificationManager manager, string channelIdentifier)
        {
            var channels = await manager.GetChannels();
            var newChannels = channels
                .Where(x => x.Identifier != channelIdentifier)
                .ToArray();
            await manager.SetChannels(newChannels);
        }



        public static async Task UpdateChannel(this INotificationManager manager, Channel channel)
        {
            await manager.DeleteChannel(channel.Identifier);
            await manager.CreateChannel(channel);
        }


        public static bool TryCreatePersistentNotification(this INotificationManager manager, Notification notification, out IPersistentNotification? persistentNotification)
        {
            persistentNotification = null;
            if (manager is IPersistentNotificationManagerExtension ext)
            {
                persistentNotification = ext.Create(notification);
                return true;
            }
            return false;
        }


        public static Task Send(this INotificationManager notifications, string title, string message, string? channel = null, DateTime? scheduleDate = null)
            => notifications.Send(new Notification
            {
                Title = title,
                Message = message,
                Channel = channel,
                ScheduleDate = scheduleDate
            });


        public static Task SetChannel(this IRepository repository, Channel channel)
            => repository.Set(channel.Identifier, channel);

        //public static Task DeleteChannel(this IRepository repository, string channelIdentifier)
        //    => repository.Remove<Channel>(channelIdentifier);

        public static Task DeleteAllChannels(this IRepository repository)
            => repository.Clear<Channel>();

        public static Task<Channel?> GetChannel(this IRepository repository, string channelIdentifier)
            => repository.Get<Channel>(channelIdentifier);

        public static Task<IList<Channel>> GetChannels(this IRepository repository)
            => repository.GetAll<Channel>();
    }
}
