using System;
using UserNotifications;


namespace Shiny.Notifications
{
    public static class PlatformExtensions
    {
        public static Notification FromNative(this UNNotificationRequest native)
        {
            Int32.TryParse(native.Identifier, out var id);

            var shiny = new Notification
            {
                Id = id,
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
