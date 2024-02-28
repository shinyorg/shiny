using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Shiny.Notifications;


public static class NotificationExtensions
{
    internal static TNotification TryToNative<TNotification>(this Notification notification) where TNotification: Notification, new()
    {
        if (notification is TNotification native)
            return native;

        return new TNotification
        {
            Id = notification.Id,
            BadgeCount = notification.BadgeCount,
            Channel = notification.Channel,
            ScheduleDate = notification.ScheduleDate,
            Thread = notification.Thread,
            RepeatInterval = notification.RepeatInterval,
            Title = notification.Title,
            Message = notification.Message,
            Geofence = notification.Geofence,
            Payload = notification.Payload,
        };
    }
    
    public static async Task<AccessState> RequestRequiredAccess(this INotificationManager notificationManager, Notification notification)
    {
        var request = AccessRequestFlags.Notification;
        if (notification.RepeatInterval != null)
            request |= AccessRequestFlags.TimeSensitivity;

        if (notification.ScheduleDate != null || notification.RepeatInterval != null)
            request |= AccessRequestFlags.TimeSensitivity;

        if (notification.Geofence != null)
            request |= AccessRequestFlags.LocationAware;

        var result = await notificationManager
            .RequestAccess(request)
            .ConfigureAwait(false);

        return result;
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
}
