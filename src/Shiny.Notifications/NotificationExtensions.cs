using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Shiny.Notifications;


public static class NotificationExtensions
{
    public static INotificationManager Notifications(this ShinyContainer container) => container.GetService<INotificationManager>();


    public static async Task<AccessState> RequestRequiredAccess(this INotificationManager notificationManager, Notification notification)
    {
        var request = AccessRequestFlags.Notification;
        if (notification.RepeatInterval != null)
            request |= AccessRequestFlags.TimeSensitivity;

        if (notification.ScheduleDate != null)
        {
            var channelId = notification.Channel ?? Channel.Default.Identifier;
            var channel = await notificationManager.GetChannel(channelId)!;

            if (channel!.Importance == ChannelImportance.High)
                request |= AccessRequestFlags.TimeSensitivity;
        }

        if (notification.Geofence != null)
            request |= AccessRequestFlags.LocationAware;

        return await notificationManager
            .RequestAccess(request)
            .ConfigureAwait(false);
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


    public static Task Send(this INotificationManager notifications, string title, string message, string? channel = null, DateTime? scheduleDate = null)
        => notifications.Send(new Notification
        {
            Title = title,
            Message = message,
            Channel = channel,
            ScheduleDate = scheduleDate
        });


    internal static Task DeleteAllChannels(this INotificationManager notificationManager, IChannelManager channelManager) => Task.WhenAll(
        notificationManager.Cancel(CancelScope.All),
        channelManager.Clear()
    );


    internal static async Task DeleteChannel(this INotificationManager notificationManager, IChannelManager channelManager, string channelId)
    {
        await channelManager
            .Remove(channelId)
            .ConfigureAwait(false);

        var pending = await notificationManager
            .GetNotificationsByChannel(channelId)
            .ConfigureAwait(false);

        foreach (var notification in pending)
            await notificationManager.Cancel(notification.Id).ConfigureAwait(false);
    }


    internal static async Task<IList<Notification>> GetNotificationsByChannel(this INotificationManager notificationManager, string channelId)
    {
        var pending = await notificationManager.GetPendingNotifications().ConfigureAwait(false);
        return pending
            .Where(x =>
                x.Channel != null &&
                x.Channel.Equals(channelId, StringComparison.InvariantCultureIgnoreCase)
            )
            .ToList();
    }
}
