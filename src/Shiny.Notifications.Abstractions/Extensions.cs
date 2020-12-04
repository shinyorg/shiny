using System;
using System.Threading.Tasks;


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
    }
}
