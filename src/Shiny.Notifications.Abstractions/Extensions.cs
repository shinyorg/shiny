using System;
using System.Threading.Tasks;


namespace Shiny.Notifications
{
    public static class Extensions
    {
        public static Task Send(this INotificationManager notifications, string title, string message, string? category = null, DateTime? scheduleDate = null)
            => notifications.Send(new Notification
            {
                Title = title,
                Message = message,
                Category = category,
                ScheduleDate = scheduleDate
            });


        public static bool IsCustomSound(this NotificationSound sound) => sound != null && sound.Type == NotificationSoundType.Custom;


        public static async Task<NotificationResult> RequestAccessAndSend(this INotificationManager notifications, Notification notification)
        {
            var access = await notifications.RequestAccess();
            if (access != AccessState.Available)
                return NotificationResult.Fail(access);

            await notifications.Send(notification);
            return NotificationResult.Success(notification.Id);
        }


        /// <summary>
        /// DO NOT use this in your delegates or background tasks - it should only be used in the foreground where permission dialogs can be presented
        /// </summary>
        /// <param name="notifications"></param>
        /// <param name="title"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        public static Task<NotificationResult> RequestAccessAndSend(this INotificationManager notifications, string title, string message, string? category = null, DateTime? scheduleDate = null)
        {
            var notification = new Notification
            {
                Title = title,
                Message = message,
                Category = category,
                ScheduleDate = scheduleDate
            };
            return notifications.RequestAccessAndSend(notification);
        }
    }


    public struct NotificationResult
    {
        public static NotificationResult Success(int id) => new NotificationResult(id, AccessState.Available);
        public static NotificationResult Fail(AccessState access) => new NotificationResult(0, access);


        NotificationResult(int notificationId, AccessState accessStatus)
        {
            this.NotificationId = notificationId;
            this.AccessStatus = accessStatus;
        }


        public int NotificationId { get; }
        public AccessState AccessStatus { get; }
    }
}
