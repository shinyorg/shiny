using System;
using Foundation;
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
                Payload = GetPayload(native),
                BadgeCount = native.Content?.Badge.Int32Value
            };
            //UNUserNotificationCenter.Current.GetNotificationCategoriesAsync();
            //native.Content.CategoryIdentifier

            if (native.Trigger is UNCalendarNotificationTrigger calendar)
                shiny.ScheduleDate = calendar.NextTriggerDate?.ToDateTime() ?? DateTime.Now;
                // if null, it is firing and it is firing right now

            return shiny;
        }


        static string? GetPayload(UNNotificationRequest request)
        {
            var userInfo = request?.Content?.UserInfo;
            if (userInfo == null)
                return null;

            var key = new NSString("Payload");
            if (!userInfo.ContainsKey(key))
                return null;

            return userInfo[key].ToString();
        }
    }
}
