using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Foundation;
using UIKit;
using UserNotifications;
using Shiny.Settings;


namespace Shiny.Notifications
{
    public class NotificationManager : INotificationManager
    {
        readonly ISettings settings; // this will have problems with data protection


        public NotificationManager(ISettings settings)
        {
            this.settings = settings;
            UNUserNotificationCenter.Current.Delegate = new ShinyNotificationDelegate();
        }


        public Task SetBadge(int value) => Dispatcher.InvokeOnMainThreadAsync(() =>
            UIApplication.SharedApplication.ApplicationIconBadgeNumber = value
        );


        public Task<int> GetBadge() => Dispatcher.InvokeOnMainThreadAsync(() =>
            (int)UIApplication.SharedApplication.ApplicationIconBadgeNumber
        );


        public Task<AccessState> RequestAccess()
        {
            var tcs = new TaskCompletionSource<AccessState>();

            UNUserNotificationCenter.Current.RequestAuthorization(
                UNAuthorizationOptions.Alert |
                UNAuthorizationOptions.Badge |
                UNAuthorizationOptions.Sound,
                (approved, error) =>
                {
                    if (error != null)
                    {
                        tcs.SetException(new Exception(error.Description));
                    }
                    else
                    {
                        var state = approved ? AccessState.Available : AccessState.Denied;
                        tcs.SetResult(state);
                    }
                });

            return tcs.Task;
        }


        public Task<IEnumerable<Notification>> GetPending() => Dispatcher.InvokeOnMainThread<IEnumerable<Notification>>(async tcs =>
        {
            var requests = await UNUserNotificationCenter
                .Current
                .GetPendingNotificationRequestsAsync();

            var notifications = requests
                .Select(x => x.FromNative())
                .Where(x => x != null);

            tcs.SetResult(notifications);
        });


        public Task Clear() => Dispatcher.InvokeOnMainThreadAsync(() =>
        {
            UNUserNotificationCenter.Current.RemoveAllPendingNotificationRequests();
            UNUserNotificationCenter.Current.RemoveAllDeliveredNotifications();
        });


        public async Task Send(Notification notification)
        {
            if (notification.Id == 0)
                notification.Id = this.settings.IncrementValue("NotificationId");

            var access = await this.RequestAccess();
            access.Assert();

            var content = new UNMutableNotificationContent
            {
                Title = notification.Title,
                Body = notification.Message
                //LaunchImageName = ""
                //Subtitle = ""
            };
            if (notification.BadgeCount != null)
                content.Badge = notification.BadgeCount.Value;

            //UNNotificationAttachment.FromIdentifier("", NSUrl.FromString(""), new UNNotificationAttachmentOptions().)
            if (!notification.Payload.IsEmpty())
            {
                var dict = new NSMutableDictionary();
                dict.Add(new NSString("Payload"), new NSString(notification.Payload));
                content.UserInfo = dict;
            }

            if (!Notification.CustomSoundFilePath.IsEmpty())
                content.Sound = UNNotificationSound.GetSound(Notification.CustomSoundFilePath);

            UNNotificationTrigger trigger = null;
            if (notification.ScheduleDate != null)
            {
                var dt = notification.ScheduleDate.Value.ToLocalTime();
                trigger = UNCalendarNotificationTrigger.CreateTrigger(new NSDateComponents
                {
                    Year = dt.Year,
                    Month = dt.Month,
                    Day = dt.Day,
                    Hour = dt.Hour,
                    Minute = dt.Minute,
                    Second = dt.Second
                }, false);
            }

            var request = UNNotificationRequest.FromIdentifier(
                notification.Id.ToString(),
                content,
                trigger
            );
            await UNUserNotificationCenter
                .Current
                .AddNotificationRequestAsync(request);
        }


        public Task Cancel(int notificationId) => Dispatcher.InvokeOnMainThreadAsync(() =>
        {
            var ids = new[] { notificationId.ToString() };

            UNUserNotificationCenter.Current.RemovePendingNotificationRequests(ids);
            UNUserNotificationCenter.Current.RemoveDeliveredNotifications(ids);
        });
    }
}
//UNUserNotificationCenter.Current.SetNotificationCategories(
//    UNNotificationCategory.FromIdentifier(
//        "",
//        new UNNotificationAction[]
//        {
//            UNNotificationAction.FromIdentifier(
//                "id",
//                "title",
//                UNNotificationActionOptions.AuthenticationRequired
//            )
//        },
//        new string[] { "" },
//        "hiddenPreviewsBodyPlaceholder",
//        new NSString(""),
//        UNNotificationCategoryOptions.None
//    )
//);