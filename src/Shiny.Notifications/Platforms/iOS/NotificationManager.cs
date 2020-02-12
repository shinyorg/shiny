using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Reactive.Linq;
using Foundation;
using UIKit;
using UserNotifications;
using Shiny.Settings;
using Shiny.Logging;


namespace Shiny.Notifications
{
    public class NotificationManager : INotificationManager, IShinyStartupTask
    {
        /// <summary>
        /// This requires a special entitlement from Apple that is general disabled for anything but healt & public safety alerts
        /// </summary>
        public static bool UseCriticalAlerts { get; set; }

        readonly ShinyNotificationContext context;
        readonly ISettings settings; // this will have problems with data protection


        public NotificationManager(ShinyNotificationContext context, ISettings settings)
        {
            this.context = context;
            this.settings = settings;
        }


        public void Start()
        {
            var sdelegate = this.context.Services.Resolve<INotificationDelegate>();
            if (sdelegate == null)
                return;

            this.context
                .WhenDidReceiveNotificationResponse()
                .Where(x => !(x.Response.Notification.Request is UNPushNotificationTrigger))
                .Subscribe(x => Log.SafeExecute(async () =>
                {
                    var notification = x.Response.Notification.Request.FromNative();

                    if (x.Response is UNTextInputNotificationResponse textResponse)
                    {
                        var shinyResponse = new NotificationResponse(notification, textResponse.ActionIdentifier, textResponse.UserText);
                        await sdelegate.OnEntry(shinyResponse);
                    }
                    else
                    {
                        var shinyResponse = new NotificationResponse(notification, x.Response.ActionIdentifier, null);
                        await sdelegate.OnEntry(shinyResponse);
                    }
                    x.CompletionHandler.Invoke();
                }));

            this.context
                .WhenWillPresentNotification()
                .Where(x => !(x.Notification.Request is UNPushNotificationTrigger))
                .Subscribe(x => Log.SafeExecute(async () =>
                {
                    var shinyNotification = x.Notification.Request.FromNative();
                    await sdelegate.OnReceived(shinyNotification);
                    x.CompletionHandler(UNNotificationPresentationOptions.Alert);
                }));
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
            var request = UNAuthorizationOptions.Alert |
                          UNAuthorizationOptions.Badge |
                          UNAuthorizationOptions.Sound;

            if (UseCriticalAlerts && UIDevice.CurrentDevice.CheckSystemVersion(12, 0))
                request |= UNAuthorizationOptions.CriticalAlert;

            UNUserNotificationCenter.Current.RequestAuthorization(
                request,
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

            var request = UNNotificationRequest.FromIdentifier(
                notification.Id.ToString(),
                this.GetContent(notification),
                this.GetTrigger(notification)
            );
            await UNUserNotificationCenter
                .Current
                .AddNotificationRequestAsync(request);
        }


        protected virtual UNNotificationContent GetContent(Notification notification)
        {
            var content = new UNMutableNotificationContent
            {
                Title = notification.Title,
                Body = notification.Message
            };
            this.SetSound(notification, content);
            if (!notification.Category.IsEmpty())
                content.CategoryIdentifier = notification.Category;

            if (notification.BadgeCount != null)
                content.Badge = notification.BadgeCount.Value;

            if (!notification.Payload.IsEmpty())
            {
                var dict = new NSMutableDictionary();
                dict.Add(new NSString("Payload"), new NSString(notification.Payload));
                content.UserInfo = dict;
            }
            return content;
        }


        protected virtual void SetSound(Notification notification, UNMutableNotificationContent content)
        {
            var s = notification.Sound;
            if (!s.Equals(NotificationSound.None))
            {
                if (s.Equals(NotificationSound.DefaultSystem))
                    content.Sound = UNNotificationSound.Default;
                else if (s.Equals(NotificationSound.DefaultPriority))
                    content.Sound = UseCriticalAlerts && UIDevice.CurrentDevice.CheckSystemVersion(12, 0)
                        ? UNNotificationSound.DefaultCriticalSound
                        : UNNotificationSound.Default;
                else
                    content.Sound = UNNotificationSound.GetSound(s.Path);
            }
        }


        protected virtual UNNotificationTrigger? GetTrigger(Notification notification)
        {
            UNNotificationTrigger? trigger = null;
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
            return trigger;
        }


        public Task Cancel(int notificationId) => Dispatcher.InvokeOnMainThreadAsync(() =>
        {
            var ids = new[] { notificationId.ToString() };

            UNUserNotificationCenter.Current.RemovePendingNotificationRequests(ids);
            UNUserNotificationCenter.Current.RemoveDeliveredNotifications(ids);
        });
    }
}
