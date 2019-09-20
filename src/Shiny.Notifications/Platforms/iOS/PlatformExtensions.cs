using System;
using UserNotifications;


namespace Shiny.Notifications
{
    public static class PlatformExtensions
    {
        public static Notification FromNative(this UNNotificationRequest native)
        {
            if (!Int32.TryParse(native.Identifier, out var i))
                return null;

            var shiny = new Notification
            {
                Id = i,
                Title = native.Content?.Title,
                Message = native.Content?.Body,
                //Metadata = native.Content.UserInfo.FromNsDictionary()
            };

            if (native.Trigger is UNCalendarNotificationTrigger calendar)
                shiny.ScheduleDate = calendar.NextTriggerDate?.ToDateTime() ?? DateTime.Now;
                // if null, it is firing and it is firing right now

            return shiny;
        }
    }
}
