using System;
using UserNotifications;


namespace Shiny.Notifications
{
    static class PlatformExtensions
    {
        public static Notification? FromNative(this UNNotificationRequest native)
        {
            if (!Int32.TryParse(native.Identifier, out var id))
                return null;

            var shiny = new Notification
            {
                Id = id,
                Title = native.Content?.Title,
                Message = native.Content?.Body,
                Payload = native.Content?.UserInfo?.FromNsDictionary(),
                BadgeCount = native.Content?.Badge?.Int32Value,
                Channel = native.Content?.CategoryIdentifier
            };

            if (native.Trigger is UNCalendarNotificationTrigger calendar)
                shiny.ScheduleDate = calendar.NextTriggerDate?.ToDateTime() ?? DateTime.Now;

            return shiny;
        }
    }
}
