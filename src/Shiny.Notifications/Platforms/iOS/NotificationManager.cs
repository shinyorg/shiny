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


        public NotificationManager(ShinyCoreServices services)
        {
            this.services = services;
        }


        public void Start()
        {
            this.services
                .Repository
                .GetChannels()
                .ContinueWith(x =>
                    this.SetChannels(x.Result.ToArray())
                );

            this.services.Lifecycle.RegisterForNotificationReceived(async response =>
            {
                var t = response.Notification?.Request?.Trigger;
                if (t == null || t is UNCalendarNotificationTrigger)
                {
                    var shiny = response.FromNative();
                    await this.services.Services.RunDelegates<INotificationDelegate>(x => x.OnEntry(shiny));
                }
            });
        }


        public int Badge
        {
            get => this.services.Settings.Get<int>("Badge");
            set => this.services.Platform.InvokeOnMainThread(() =>
            {
                UIApplication.SharedApplication.ApplicationIconBadgeNumber = value;
                this.services.Settings.Set("Badge", value);
            });
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


        public Task<IEnumerable<Notification>> GetPending() => this.services.Platform.InvokeOnMainThreadAsync(async () =>
        {
            var requests = await UNUserNotificationCenter
                .Current
                .GetPendingNotificationRequestsAsync();

            var notifications = requests.Select(x => x.FromNative());
            return notifications;
        });


        public Task Clear() => this.services.Platform.InvokeOnMainThreadAsync(() =>
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


        public Task Cancel(int notificationId) => this.services.Platform.InvokeOnMainThreadAsync(() =>
        {
            var ids = new[] { notificationId.ToString() };

            UNUserNotificationCenter.Current.RemovePendingNotificationRequests(ids);
            UNUserNotificationCenter.Current.RemoveDeliveredNotifications(ids);
        });


        public Task<IList<Channel>> GetChannels()
            => this.services.Repository.GetChannels();


        public async Task AddChannel(Channel channel)
        {
            channel.AssertValid();
            await this.services.Repository.SetChannel(channel);
            await this.RebuildNativeCategories();
        }


        public async Task RemoveChannel(string channelId)
        {
            await this.services.Repository.RemoveChannel(channelId);
            await this.RebuildNativeCategories();
        }


        public Task ClearChannels() => this.services.Repository.RemoveAllChannels();


        protected async Task RebuildNativeCategories()
        {
            var channels = await this.services.Repository.GetChannels();
            var list = channels.ToList();
            list.Add(Channel.Default);

            var categories = new List<UNNotificationCategory>();
            foreach (var channel in list)
            {
                var actions = new List<UNNotificationAction>();
                foreach (var action in channel.Actions)
                {
                    var nativeAction = this.CreateAction(action);
                    actions.Add(nativeAction);
                }

                var native = UNNotificationCategory.FromIdentifier(
                    channel.Identifier,
                    actions.ToArray(),
                    new string[] { "" },
                    UNNotificationCategoryOptions.None
                );
                categories.Add(native);
                await this.services.Repository.SetChannel(channel);
            }
            var set = new NSSet<UNNotificationCategory>(categories.ToArray());
            UNUserNotificationCenter.Current.SetNotificationCategories(set);
        }


        protected virtual UNNotificationAction CreateAction(ChannelAction action) => action.ActionType switch
        {
            ChannelActionType.TextReply => UNTextInputNotificationAction.FromIdentifier(
                action.Identifier,
                action.Title,
                UNNotificationActionOptions.None,
                action.Title,
                String.Empty
            ),

            ChannelActionType.Destructive => UNNotificationAction.FromIdentifier(
                action.Identifier,
                action.Title,
                UNNotificationActionOptions.Destructive
            ),

            ChannelActionType.OpenApp => UNNotificationAction.FromIdentifier(
                action.Identifier,
                action.Title,
                UNNotificationActionOptions.Foreground
            ),

            ChannelActionType.None => UNNotificationAction.FromIdentifier(
                action.Identifier,
                action.Title,
                UNNotificationActionOptions.None
            )
        };


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
            var channel = Channel.Default;
            if (!notification.Channel.IsEmpty())
            {
                channel = await this.services.Repository.GetChannel(notification.Channel);
                if (channel == null)
                    throw new ArgumentException($"{notification.Channel} does not exist");
            }

            native.CategoryIdentifier = channel.Identifier;
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
    }
}
