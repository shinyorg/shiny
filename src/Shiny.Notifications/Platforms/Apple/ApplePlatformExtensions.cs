using System;
using Shiny.Locations;
using UserNotifications;

namespace Shiny.Notifications;


public static class ApplePlatformExtensions
{
    public static AppleNotification FromNative(this UNNotificationRequest native)
    {
        var id = 0;
        Int32.TryParse(native.Identifier, out id);

        var shiny = new AppleNotification
        {
            Id = id,
            Title = native.Content?.Title,
            Message = native.Content?.Body,
            Payload = native.Content?.UserInfo?.FromNsDictionary(),
            BadgeCount = native.Content?.Badge?.Int32Value,
            Channel = native.Content?.CategoryIdentifier,            
            TargetContentIdentifier = native.Content?.TargetContentIdentifier,
            Subtitle = native.Content?.Subtitle
        };


        if (OperatingSystemShim.IsAppleVersionAtleast(16))
        {
            if (native.Content?.RelevanceScore > 0)
                shiny.RelevanceScore = native.Content!.RelevanceScore!;

            shiny.FilterCriteria = native.Content?.FilterCriteria!;
        }

        if (native.Trigger is UNCalendarNotificationTrigger calendar)
        {
            if (!calendar.Repeats)
            {
                shiny.ScheduleDate = calendar.NextTriggerDate?.ToDateTime() ?? DateTime.Now;
            }
            else
            {
                var dc = calendar.DateComponents;

                shiny.RepeatInterval = new IntervalTrigger
                {
                    TimeOfDay = new TimeSpan(
                        (int)dc.Hour,
                        (int)dc.Minute,
                        (int)dc.Second
                    )
                };
                if (dc.Weekday < 8)
                {
                    shiny.RepeatInterval.DayOfWeek = (DayOfWeek)(int)(dc.Weekday - 1);
                }
            }
        }
        else if (native.Trigger is UNTimeIntervalNotificationTrigger interval)
        {
            shiny.RepeatInterval = new IntervalTrigger
            {
                Interval = TimeSpan.FromSeconds(interval.TimeInterval)
            };
            shiny.ScheduleDate = interval.NextTriggerDate!.ToDateTime();
        }
#if IOS
        else if (native.Trigger is UNLocationNotificationTrigger location)
        {
            shiny.Geofence = new GeofenceTrigger
            {
                Center = new Position(location.Region.Center.Latitude, location.Region.Center.Longitude),
                Radius = Distance.FromMeters(location.Region.Radius)
            };
        }
#endif
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
