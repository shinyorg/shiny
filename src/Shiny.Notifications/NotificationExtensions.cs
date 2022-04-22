using System;
using System.Reflection;
using System.Threading.Tasks;


namespace Shiny.Notifications
{
    public static class NotificationExtensions
    {
        // TODO
        public static async Task<AccessState> RequestRequiredAccess(this INotificationManager notificationManager, Notification notification)
        {
            var request = AccessRequestFlags.Notification;
            if (notification.RepeatInterval != null)
                request |= AccessRequestFlags.TimeSensitivity;

            if (!notification.Channel.IsEmpty())
            {
                //var channel = await notificationManager
                //    .GetChannel(notification.Channel!)
                //    .ConfigureAwait(false);

                //var high = (channel?.Importance ?? ChannelImportance.Low) >= ChannelImportance.High;

                //if (notification.ScheduleDate != null && high)
                if (notification.ScheduleDate != null)
                    request |= AccessRequestFlags.TimeSensitivity;
            }

            if (notification.Geofence != null)
                request |= AccessRequestFlags.LocationAware;

            return await notificationManager.RequestAccess(request).ConfigureAwait(false);
        }


        public static void AssertValid(this Notification notification)
        {
            var triggers = 0;
            triggers += notification.ScheduleDate == null ? 0 : 1;
            triggers += notification.Geofence == null ? 0 : 1;
            triggers += notification.RepeatInterval == null ? 0 : 1;

            if (triggers > 1)
                throw new InvalidOperationException("You cannot mix scheduled date, repeated interval, and/or geofences on a notification");

            if (triggers > 0 && notification.BadgeCount > 0)
                throw new InvalidOperationException("BadgeCount is not respected for triggered notifications");

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


        public static Task Send(this INotificationManager notifications, string title, string message, string? channel = null, DateTime? scheduleDate = null)
            => notifications.Send(new Notification
            {
                Title = title,
                Message = message,
                Channel = channel,
                ScheduleDate = scheduleDate
            });


        //internal static async Task RemoveChannel(this IRepository repository, string channelId)
        //{
        //    await repository.Remove<Channel>(channelId);
        //    var notifications = await repository.GetNotificationsByChannel(channelId);

        //    foreach (var notification in notifications)
        //    {
        //        notification.Channel = null;
        //        await repository.Set(notification.Id.ToString(), notification);
        //    }
        //}


        //internal static async Task RemoveAllChannels(this IRepository repository)
        //{
        //    await repository.Clear<Channel>();
        //    var notifications = await repository.GetList<Notification>(x => !x.Channel.IsEmpty());

        //    foreach (var notification in notifications)
        //    {
        //        notification.Channel = null;
        //        await repository.Set(notification.Id.ToString(), notification);
        //    }
        //}


        //internal static async Task RemoveChannelFromPendingNotificationsByChannel(this IRepository repository, string channelId)
        //{
        //    var notifications = await repository.GetNotificationsByChannel(channelId);
        //    foreach (var notification in notifications)
        //    {
        //        notification.Channel = null;
        //        await repository.Set(notification.Id.ToString(), notification);
        //    }
        //}


        //internal static Task<IList<Notification>> GetNotificationsByChannel(this IRepository repository, string channelId)
        //    => repository.GetList<Notification>(x =>
        //         x.Channel != null && x.Channel.Equals(channelId, StringComparison.InvariantCultureIgnoreCase)
        //    );
    }
}
