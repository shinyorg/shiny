using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Shiny.Infrastructure;


namespace Shiny.Notifications
{
    public static class Extensions
    {
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

        public static Task DeleteChannel(this IRepository repository, string channelIdentifier)
            => repository.Remove<Channel>(channelIdentifier);

        public static Task<Channel?> GetChannel(this IRepository repository, string channelIdentifier)
            => repository.Get<Channel>(channelIdentifier);

        public static Task<IList<Channel>> GetChannels(this IRepository repository)
            => repository.GetAll<Channel>();
    }
}
