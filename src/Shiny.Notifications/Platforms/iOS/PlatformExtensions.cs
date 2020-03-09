using System;
using Foundation;
using UserNotifications;


namespace Shiny.Notifications
{
    public static class PlatformExtensions
    {
        public static Notification? FromNative(this UNNotificationRequest native)
        {
            if (!Int32.TryParse(native.Identifier, out var i))
                return null;

            var shiny = new Notification
            {
                Id = i,
                Title = native.Content?.Title,
                Message = native.Content?.Body,
                Payload = native.Content?.UserInfo?.FromNsDictionary(),
                BadgeCount = native.Content?.Badge?.Int32Value,
                Category = native.Content?.CategoryIdentifier
            };

            if (native.Trigger is UNCalendarNotificationTrigger calendar)
                shiny.ScheduleDate = calendar.NextTriggerDate?.ToDateTime() ?? DateTime.Now;

            return shiny;
        }
    }
}
