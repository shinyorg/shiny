using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Reactive.Linq;
using Foundation;
using UIKit;
using UserNotifications;
using Shiny.Infrastructure;
using CoreLocation;


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

                // TODO
                if (t == null || t is UNCalendarNotificationTrigger)
                {
                    var shiny = response.FromNative();
                    await this.services
                        .Services
                        .RunDelegates<INotificationDelegate>(x => x.OnEntry(shiny))
                        .ConfigureAwait(false);
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


        public Task<AccessState> RequestAccess(AccessRequestFlags access)
        {
            var tcs = new TaskCompletionSource<AccessState>();
            var request = UNAuthorizationOptions.Alert |
                          UNAuthorizationOptions.Badge |
                          UNAuthorizationOptions.Sound;

            if (access.HasFlag(AccessRequestFlags.TimeSensitivity))
                request |= UNAuthorizationOptions.TimeSensitive;

            //UNAuthorizationOptions.Announcement | UNAuthorizationOptions.CarPlay
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
                .GetPendingNotificationRequestsAsync()
                .ConfigureAwait(false);

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
            notification.AssertValid();

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
                .AddNotificationRequestAsync(request)
                .ConfigureAwait(false);
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
            await this.services.Repository.SetChannel(channel).ConfigureAwait(false);
            await this.RebuildNativeCategories().ConfigureAwait(false);
        }


        public async Task RemoveChannel(string channelId)
        {
            await this.services.Repository.RemoveChannel(channelId).ConfigureAwait(false);
            await this.RebuildNativeCategories().ConfigureAwait(false);
        }


        public Task ClearChannels() => this.services.Repository.RemoveAllChannels();


        protected async Task RebuildNativeCategories()
        {
            var channels = await this.services
                .Repository
                .GetChannels()
                .ConfigureAwait(false);

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
                await this.services
                    .Repository
                    .SetChannel(channel)
                    .ConfigureAwait(false);
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
                Body = notification.Message,
                ThreadIdentifier = notification.Thread
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
                    throw new InvalidOperationException($"{notification.Channel} does not exist");
            }

            if (UIDevice.CurrentDevice.CheckSystemVersion(15, 0))
            {
                native.InterruptionLevel = channel.Importance switch 
                {
                    ChannelImportance.Low => UNNotificationInterruptionLevel.Passive,
                    ChannelImportance.High => UNNotificationInterruptionLevel.TimeSensitive,
                    ChannelImportance.Critical => UNNotificationInterruptionLevel.Critical,
                    _ => UNNotificationInterruptionLevel.Active
                };
            }

            native.CategoryIdentifier = channel.Identifier;
            if (!channel.CustomSoundPath.IsEmpty())
            {
                if (channel.Importance == ChannelImportance.Critical)
                {
                    native.Sound = UNNotificationSound.GetCriticalSound(channel.CustomSoundPath);
                }
                else
                {
                    native.Sound = UNNotificationSound.GetSound(channel.CustomSoundPath);
                }
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

            if (notification.Geofence != null)
            {
                var geo = notification.Geofence!;

                trigger = UNLocationNotificationTrigger.CreateTrigger(new CLRegion(
                    new CLLocationCoordinate2D(geo.Center!.Latitude, geo.Center!.Longitude),
                    geo.Radius!.TotalMeters,
                    notification.Id.ToString()
                ), geo.Repeat);
            }
            else if (notification.ScheduleDate != null)
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
            else if (notification.RepeatInterval != null)
            {
                var tcfg = notification.RepeatInterval!;
                if (tcfg.Interval != null)
                {
                    trigger = UNTimeIntervalNotificationTrigger.CreateTrigger(tcfg.Interval.Value.TotalSeconds, true);
                }
                else
                {
                    var component = new NSDateComponents
                    {
                        Hour = tcfg.TimeOfDay!.Value.Hours,
                        Minute = tcfg.TimeOfDay!.Value.Minutes,
                        Second = tcfg.TimeOfDay!.Value.Seconds
                    };
                    if (tcfg.DayOfWeek != null)
                        component.Weekday = (int)tcfg.DayOfWeek + 1;

                    trigger = UNCalendarNotificationTrigger.CreateTrigger(component, true);
                }
            }
            
            return trigger;
        }
    }
}
