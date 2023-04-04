namespace Shiny.Notifications;


public static class InternalExtensions
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
            LocalAttachmentPath = notification.LocalAttachmentPath,
            Message = notification.Message,
            Geofence = notification.Geofence,
            Payload = notification.Payload,
        };
    }
}