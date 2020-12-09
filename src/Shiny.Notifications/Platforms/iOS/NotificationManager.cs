using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Reactive.Linq;
using Foundation;
using UIKit;
using UserNotifications;
using Shiny.Infrastructure;


namespace Shiny.Notifications
{
    public class NotificationManager : INotificationManager, IShinyStartupTask
    {
        /// <summary>
        /// This requires a special entitlement from Apple that is general disabled for anything but healt & public safety alerts
        /// </summary>
        public static bool UseCriticalAlerts { get; set; }

        readonly ShinyCoreServices services;
        readonly iOSNotificationDelegate nativeDelegate;


        public NotificationManager(ShinyCoreServices services, iOSNotificationDelegate nativeDelegate)
        {
            this.services = services;
            this.nativeDelegate = nativeDelegate;
        }


        public void Start()
        {
            this.SetCategories();

            this.nativeDelegate
                .WhenPresented()
                .Where(x => !(x.Notification?.Request?.Trigger is UNPushNotificationTrigger))
                .SubscribeAsync(async x =>
                {
                    var shiny = x.Notification.Request.FromNative();
                    if (shiny != null)
                    {
                        await this.services.Services.RunDelegates<INotificationDelegate>(x => x.OnReceived(shiny));
                        x.CompletionHandler.Invoke(UNNotificationPresentationOptions.Alert);
                    }
                });

            this.nativeDelegate
                .WhenResponse()
                .Where(x => !(x.Response.Notification?.Request?.Trigger is UNPushNotificationTrigger))
                .SubscribeAsync(async x =>
                {
                    var shiny = x.Response.Notification.Request.FromNative();
                    if (shiny != null)
                    {
                        NotificationResponse response = default;
                        if (x.Response is UNTextInputNotificationResponse textResponse)
                        {
                            response = new NotificationResponse(
                                shiny,
                                textResponse.ActionIdentifier,
                                textResponse.UserText
                            );
                        }
                        else
                        {
                            response = new NotificationResponse(shiny, x.Response.ActionIdentifier, null);
                        }

                        await this.services.Services.RunDelegates<INotificationDelegate>(x => x.OnEntry(response));
                        x.CompletionHandler();
                    }
                });
        }


        public int Badge
        {
            get => this.services.Settings.Get("Badge", 0);
            set
            {
                this.services.Settings.Set("Badge", value);
                Dispatcher.InvokeOnMainThreadAsync(() =>
                    UIApplication.SharedApplication.ApplicationIconBadgeNumber = value
                );
            }
        }


        public Task<AccessState> RequestAccess()
        {
            var tcs = new TaskCompletionSource<AccessState>();
            var request = UNAuthorizationOptions.Alert |
                          UNAuthorizationOptions.Badge |
                          UNAuthorizationOptions.Sound;

            // https://medium.com/@shashidharyamsani/implementing-ios-critical-alerts-7d82b4bb5026
    //        {
    //“aps” : {
    //    “sound” : {
    //        “critical”: 1,
    //        “name”: “critical - alert - sound.wav”,
    //        “volume”: 1.0
    //    }
    //            }
    //        }
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
                notification.Id = this.services.Settings.IncrementValue("NotificationId");

            var content = await this.GetContent(notification);
            var request = UNNotificationRequest.FromIdentifier(
                notification.Id.ToString(),
                content,
                this.GetTrigger(notification)
            );
            await UNUserNotificationCenter
                .Current
                .AddNotificationRequestAsync(request);
        }


        protected virtual async Task<UNMutableNotificationContent> GetContent(Notification notification)
        {
            var content = new UNMutableNotificationContent
            {
                Title = notification.Title,
                Body = notification.Message
            };
            if (notification.BadgeCount != null)
                content.Badge = notification.BadgeCount.Value;

            if (!notification.Payload.IsEmpty())
                content.UserInfo = notification.Payload.ToNsDictionary();

            await this.ApplyChannel(notification, content);
            return content;
        }


        protected virtual async Task ApplyChannel(Notification notification, UNMutableNotificationContent native)
        {
            if (notification.Channel.IsEmpty())
                return;

            var channel = await this.services.Repository.Get<Channel>(notification.Channel);
            if (channel == null)
                return;

            native.CategoryIdentifier = notification.Channel;
            if (!channel.CustomSoundPath.IsEmpty())
            {
                native.Sound = UNNotificationSound.GetSound(channel.CustomSoundPath);
            }
            else
            {
                switch (channel.Importance)
                {
                    case ChannelImportance.Critical:
                    case ChannelImportance.High:
                        native.Sound = UseCriticalAlerts && UIDevice.CurrentDevice.CheckSystemVersion(12, 0)
                            ? UNNotificationSound.DefaultCriticalSound
                            : UNNotificationSound.Default;
                        break;

                    case ChannelImportance.Normal:
                        native.Sound = UNNotificationSound.Default;
                        break;

                    case ChannelImportance.Low:
                        break;
                }
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


        public async Task CreateChannel(Channel channel)
        {
            await this.services.Repository.SetChannel(channel);
            await this.SetCategories();
        }


        public async Task DeleteChannel(string identifier)
        {
            await this.services.Repository.DeleteChannel(identifier);
            await this.SetCategories();
        }


        public Task<IList<Channel>> GetChannels() => this.services.Repository.GetChannels();


        async Task SetCategories()
        {
            var channels = await this.services.Repository.GetAll<Channel>();
            if (channels.Count == 0)
                return;

            var categories = new List<UNNotificationCategory>();
            foreach (var channel in channels)
            {
                var actions = new List<UNNotificationAction>();
                foreach (var action in channel.Actions)
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
                    channel.Identifier,
                    actions.ToArray(),
                    new string[] { "" },
                    UNNotificationCategoryOptions.None
                );
                categories.Add(native);
            }
            var set = new NSSet<UNNotificationCategory>(categories.ToArray());
            UNUserNotificationCenter.Current.SetNotificationCategories(set);
        }
    }
}
