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
        /// This requires a special entitlement from Apple that is general disabled for anything but health & public safety alerts
        /// </summary>
        public static bool UseCriticalAlerts { get; set; }

        readonly ShinyCoreServices services;
        readonly IChannelManager channelManager;


        public NotificationManager(ShinyCoreServices services, IChannelManager channelManager)
        {
            this.services = services;
            this.channelManager = channelManager;
        }


        public void Start()
        {
            this.services.Lifecycle.RegisterForOnFinishedLaunching(options =>
            {
                if (options.ContainsKey(UIApplication.LaunchOptionsLocalNotificationKey))
                {
                    var data = options[UIApplication.LaunchOptionsRemoteNotificationKey] as NSDictionary;
                    if (data != null)
                    {
                        // TODO: need to parse this back into a notification
                        data.FromNsDictionary();
                    }
                }
            });

            this.services.Lifecycle.RegisterForNotificationReceived(async response =>
            {
                var t = response.Notification?.Request?.Trigger;

                if (t == null || t is not UNPushNotificationTrigger)
                {
                    var shiny = response.FromNative();
                    await this.services
                        .Services
                        .RunDelegates<INotificationDelegate>(x => x.OnEntry(shiny))
                        .ConfigureAwait(false);
                }
            });
        }


        public Task<Channel?> GetChannel(string channelId) => this.channelManager.Get(channelId);
        public Task AddChannel(Channel channel) => this.channelManager.Add(channel);
        public Task RemoveChannel(string channelId) => this.DeleteChannel(this.channelManager, channelId);
        public Task ClearChannels() => this.DeleteAllChannels(this.channelManager);
        public Task<IList<Channel>> GetChannels() => this.channelManager.GetAll();


        public Task<int> GetBadge() => this.services.Platform.InvokeOnMainThreadAsync(() =>
            (int)UIApplication.SharedApplication.ApplicationIconBadgeNumber
        );


        public Task SetBadge(int? badge) => this.services.Platform.InvokeOnMainThreadAsync(() =>
            UIApplication.SharedApplication.ApplicationIconBadgeNumber = badge ?? 0
        );


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


        public async Task<Notification?> GetNotification(int notificationId)
            => (await this.GetPendingNotifications()).FirstOrDefault(x => x.Id == notificationId);


        public Task<IEnumerable<Notification>> GetPendingNotifications() => this.services.Platform.InvokeTaskOnMainThread(async () =>
        {
            var requests = await UNUserNotificationCenter
                .Current
                .GetPendingNotificationRequestsAsync()
                .ConfigureAwait(false);

            var notifications = requests.Select(x => x.FromNative());
            return notifications;
        });


        public Task Send(Notification notification) => this.services.Platform.InvokeTaskOnMainThread(async () =>
        {
            notification.AssertValid();
            (await this.RequestRequiredAccess(notification).ConfigureAwait(false)).Assert();

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
        });


        public Task Cancel(CancelScope scope) => this.services.Platform.InvokeOnMainThreadAsync(() =>
        {
            if (scope == CancelScope.All || scope == CancelScope.Pending)
                UNUserNotificationCenter.Current.RemoveAllPendingNotificationRequests();

            if (scope == CancelScope.All || scope == CancelScope.DisplayedOnly)
                UNUserNotificationCenter.Current.RemoveAllDeliveredNotifications();
        });


        public Task Cancel(int notificationId) => this.services.Platform.InvokeOnMainThreadAsync(() =>
        {
            var ids = new[] { notificationId.ToString() };

            UNUserNotificationCenter.Current.RemovePendingNotificationRequests(ids);
            UNUserNotificationCenter.Current.RemoveDeliveredNotifications(ids);
        });


        protected virtual async Task<UNMutableNotificationContent> GetContent(Notification notification)
        {
            var content = new UNMutableNotificationContent
            {
                Title = notification.Title,
                Body = notification.Message
            };
            if (notification.Attachment != null)
            {
                var uri = NSUrl.FromFilename(notification.Attachment.FullName);
                var attachment = UNNotificationAttachment.FromIdentifier(
                    Guid.NewGuid().ToString(),
                    uri,
                    new UNNotificationAttachmentOptions(),
                    out var error
                );
                if (error != null)
                    throw new InvalidOperationException("Error creating notification image attachment: " + error.Description);

                content.Attachments = new[] { attachment };
            }
            if (!notification.Thread.IsEmpty())
                content.ThreadIdentifier = notification.Thread!;

            if (notification.BadgeCount != null)
                content.Badge = notification.BadgeCount.Value;

            if (!notification.Payload!.IsEmpty())
                content.UserInfo = notification.Payload!.ToNsDictionary();

            await this.ApplyChannel(notification, content);
            return content;
        }


        protected virtual async Task ApplyChannel(Notification notification, UNMutableNotificationContent native)
        {
            var channel = Channel.Default;

            if (!notification.Channel.IsEmpty())
            {
                channel = await this.channelManager.Get(notification.Channel!);
                if (channel == null)
                    throw new InvalidOperationException($"{notification.Channel} does not exist");
            }

            if (UIDevice.CurrentDevice.CheckSystemVersion(15, 0))
            {
                if (channel.Importance == ChannelImportance.High && notification.ScheduleDate != null)
                {
                    native.InterruptionLevel = UNNotificationInterruptionLevel.TimeSensitive;
                }
                else
                {
                    native.InterruptionLevel = channel.Importance switch
                    {
                        ChannelImportance.Low => UNNotificationInterruptionLevel.Passive,
                        ChannelImportance.Critical => UNNotificationInterruptionLevel.Critical,
                        _ => UNNotificationInterruptionLevel.Active
                    };
                }
            }

            native.CategoryIdentifier = channel.Identifier;
            if (!channel.CustomSoundPath.IsEmpty())
            {
                if (channel.Importance == ChannelImportance.Critical)
                {
                    native.Sound = UNNotificationSound.GetCriticalSound(channel.CustomSoundPath!);
                }
                else
                {
                    native.Sound = UNNotificationSound.GetSound(channel.CustomSoundPath!);
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

                trigger = UNLocationNotificationTrigger.CreateTrigger(
                    new CLRegion(
                        new CLLocationCoordinate2D(geo.Center!.Latitude, geo.Center!.Longitude),
                        geo.Radius!.TotalMeters,
                        notification.Id.ToString()
                    ),
                    geo.Repeat
                );
            }
            else if (notification.ScheduleDate != null)
            {
                var dt = notification.ScheduleDate.Value.ToLocalTime();
                trigger = UNCalendarNotificationTrigger.CreateTrigger(
                    new NSDateComponents
                    {
                        Year = dt.Year,
                        Month = dt.Month,
                        Day = dt.Day,
                        Hour = dt.Hour,
                        Minute = dt.Minute,
                        Second = dt.Second
                    },
                    false
                );
            }
            else if (notification.RepeatInterval != null)
            {
                var tcfg = notification.RepeatInterval!;
                if (tcfg.Interval != null)
                {
                    trigger = UNTimeIntervalNotificationTrigger.CreateTrigger(
                        tcfg.Interval.Value.TotalSeconds,
                        true
                    );
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
