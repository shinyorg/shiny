using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Foundation;
using Shiny.Settings;
using UIKit;
using UserNotifications;


namespace Shiny.Notifications
{
    public class NotificationManager : INotificationManager, IShinyStartupTask
    {
        readonly ISettings settings; // this will have problems with data protection
        public NotificationManager(ISettings settings) => this.settings = settings;
        public void Start() => UNUserNotificationCenter.Current.Delegate = new ShinyNotificationDelegate();


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


        public Task<IEnumerable<Notification>> GetPending()
        {
            var tcs = new TaskCompletionSource<IEnumerable<Notification>>();
            UIApplication.SharedApplication.BeginInvokeOnMainThread(async () =>
            {
                try
                {
                    var requests = await UNUserNotificationCenter
                        .Current
                        .GetPendingNotificationRequestsAsync();

                    var notifications = requests
                        .Select(x => x.FromNative())
                        .Where(x => x != null);
                    tcs.TrySetResult(notifications);
                }
                catch (Exception ex)
                {
                    tcs.TrySetException(ex);
                }
            });
            return tcs.Task;
        }


        public Task Clear() => this.Invoke(() =>
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
                //Badge=
                //LaunchImageName = ""
                //Subtitle = ""
            };
            //UNNotificationAttachment.FromIdentifier("", NSUrl.FromString(""), new UNNotificationAttachmentOptions().)
            if (!notification.Payload.IsEmpty())
            {
                var dict = new NSMutableDictionary();
                dict.Add(new NSString("Payload"), new NSString(notification.Payload));
                content.UserInfo = dict;
            }

            if (!notification.Sound.IsEmpty())
                content.Sound = UNNotificationSound.GetSound(notification.Sound);

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


        public Task Cancel(int notificationId) => this.Invoke(() =>
        {
            var ids = new[] { notificationId.ToString() };

            UNUserNotificationCenter.Current.RemovePendingNotificationRequests(ids);
            UNUserNotificationCenter.Current.RemoveDeliveredNotifications(ids);
        });


        protected Task Invoke(Action action)
        {
            var tcs = new TaskCompletionSource<object>();
            var app = UIApplication.SharedApplication;
            app.BeginInvokeOnMainThread(() =>
            {
                try
                {
                    action();
                    tcs.TrySetResult(null);
                }
                catch (Exception ex)
                {
                    tcs.TrySetException(ex);
                }
            });
            return tcs.Task;
        }
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