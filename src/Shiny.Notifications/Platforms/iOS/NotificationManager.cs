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


        public int Badge
        {
            get => this.settings.Get("Badge", 0);
            set
            {
                this.settings.Set("Badge", value);
                Dispatcher.InvokeOnMainThreadAsync(() =>
                    UIApplication.SharedApplication.ApplicationIconBadgeNumber = value
                );
            }
        }


        public void RegisterCategory(NotificationCategory category)
        {
            var actions = new List<UNNotificationAction>();
            foreach (var action in category.Actions)
            {
                switch (action.ActionType)
                {
                    case NotificationActionType.TextReply:
                        actions.Add(UNTextInputNotificationAction.FromIdentifier(
                            action.Identifier,
                            action.Title,
                            UNNotificationActionOptions.None,
                            action.Title,
                            String.Empty
                        ));
                        break;

                    case NotificationActionType.Destructive:
                        actions.Add(UNNotificationAction.FromIdentifier(
                            action.Identifier,
                            action.Title,
                            UNNotificationActionOptions.Destructive
                        ));
                        break;

                    case NotificationActionType.OpenApp:
                        actions.Add(UNNotificationAction.FromIdentifier(
                            action.Identifier,
                            action.Title,
                            UNNotificationActionOptions.Foreground
                        ));
                        break;

                    case NotificationActionType.None:
                        actions.Add(UNNotificationAction.FromIdentifier(
                            action.Identifier,
                            action.Title,
                            UNNotificationActionOptions.None
                        ));
                        break;
                }
            }

            var native = UNNotificationCategory.FromIdentifier(
                category.Identifier,
                actions.ToArray(),
                new string[] { "" },
                UNNotificationCategoryOptions.None
            );

            var set = new NSSet<UNNotificationCategory>(new[] { native });
            UNUserNotificationCenter.Current.SetNotificationCategories(set);
        }


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
                Body = notification.Message,
                CategoryIdentifier = notification.Category
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
