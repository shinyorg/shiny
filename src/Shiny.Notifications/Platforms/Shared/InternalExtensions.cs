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

            // TODO
        };
    }
}