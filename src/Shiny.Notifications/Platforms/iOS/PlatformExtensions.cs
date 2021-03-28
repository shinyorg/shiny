using System;
using UserNotifications;


namespace Shiny.Notifications
{
    public static class PlatformExtensions
    {
        public static Notification FromNative(this UNNotificationRequest native)
        {
            var id = 0;
            Int32.TryParse(native.Identifier, out id);

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


        public static NotificationResponse FromNative(this UNNotificationResponse native)
        {
            var shiny = native.Notification.Request.FromNative();
            NotificationResponse response = default;

            if (native is UNTextInputNotificationResponse textResponse)
            {
                response = new NotificationResponse(
                    shiny,
                    textResponse.ActionIdentifier,
                    textResponse.UserText
                );
            }
            else
            {
                response = new NotificationResponse(shiny, native.ActionIdentifier, null);
            }
            return response;
        }
    }
}
