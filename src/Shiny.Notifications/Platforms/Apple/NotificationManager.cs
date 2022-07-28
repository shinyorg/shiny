using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Reactive.Linq;
using Foundation;
using UIKit;
using UserNotifications;
using CoreLocation;
using Shiny.Hosting;
using Shiny.Stores;

namespace Shiny.Notifications;


public class NotificationManager : INotificationManager, IIosLifecycle.INotificationHandler, IIosLifecycle.IOnFinishedLaunching
{
    readonly Lazy<IEnumerable<INotificationDelegate>> delegates;
    readonly IEnumerable<INotificationCustomizer> customizers;
    readonly IosConfiguration configuration;
    readonly IPlatform platform;
    readonly IChannelManager channelManager;
    readonly IKeyValueStore settings;


    public NotificationManager(
        IosConfiguration configuration,
        IServiceProvider services,
        IPlatform platform,
        IChannelManager channelManager,
        IKeyValueStoreFactory keystore,
        IEnumerable<INotificationCustomizer> customizers
    )
    {
        this.configuration = configuration;
        this.delegates = services.GetLazyService<IEnumerable<INotificationDelegate>>();
        this.platform = platform;
        this.channelManager = channelManager;
        this.settings = keystore.DefaultStore;
        this.customizers = customizers;
    }


    public Task AddChannel(Channel channel) => this.channelManager.Add(channel);
    public Task RemoveChannel(string channelId) => this.DeleteChannel(this.channelManager, channelId);
    public Task ClearChannels() => this.DeleteAllChannels(this.channelManager);
    public Task<Channel?> GetChannel(string channelId) => this.channelManager.Get(channelId);
    public Task<IList<Channel>> GetChannels() => this.channelManager.GetAll();


    public Task<int> GetBadge() => this.platform.InvokeOnMainThreadAsync<int>(() =>
        (int)UIApplication.SharedApplication.ApplicationIconBadgeNumber
    );


    public Task SetBadge(int? badge) => this.platform.InvokeOnMainThreadAsync(() =>
        UIApplication.SharedApplication.ApplicationIconBadgeNumber = badge ?? 0
    );


    public Task<AccessState> RequestAccess(AccessRequestFlags access)
    {
        var tcs = new TaskCompletionSource<AccessState>();

        UNUserNotificationCenter.Current.RequestAuthorization(
            this.configuration.UNAuthorizationOptions,
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


    public Task<IEnumerable<Notification>> GetPendingNotifications() => this.platform.InvokeTaskOnMainThread(async () =>
    {
        var requests = await UNUserNotificationCenter
            .Current
            .GetPendingNotificationRequestsAsync()
            .ConfigureAwait(false);

        var notifications = requests.Select(x => x.FromNative());
        return notifications;
    });


    public Task Send(Notification notification) => this.platform.InvokeTaskOnMainThread(async () =>
    {
        notification.AssertValid();

        if (notification.Id == 0)
            notification.Id = this.settings.IncrementValue("NotificationId");

        var channel = Channel.Default;
        if (!notification.Channel.IsEmpty())
        {
            channel = await this.channelManager.Get(notification.Channel!);
            if (channel == null)
                throw new InvalidOperationException($"{notification.Channel} does not exist");
        }

        var content = await this
            .GetContent(notification, channel)
            .ConfigureAwait(false);

        var request = UNNotificationRequest.FromIdentifier(
            notification.Id.ToString(),
            content,
            this.GetTrigger(notification)
        );
        foreach (var customizer in this.customizers)
            await customizer.Customize(notification, channel, request).ConfigureAwait(false);

        await UNUserNotificationCenter
            .Current
            .AddNotificationRequestAsync(request)
            .ConfigureAwait(false);
    });


    public Task Cancel(CancelScope scope) => this.platform.InvokeOnMainThreadAsync(() =>
    {
        if (scope == CancelScope.All || scope == CancelScope.Pending)
            UNUserNotificationCenter.Current.RemoveAllPendingNotificationRequests();

        if (scope == CancelScope.All || scope == CancelScope.DisplayedOnly)
            UNUserNotificationCenter.Current.RemoveAllDeliveredNotifications();
    });


    public Task Cancel(int notificationId) => this.platform.InvokeOnMainThreadAsync(() =>
    {
        var ids = new[] { notificationId.ToString() };

        UNUserNotificationCenter.Current.RemovePendingNotificationRequests(ids);
        UNUserNotificationCenter.Current.RemoveDeliveredNotifications(ids);
    });


    protected virtual async Task<UNMutableNotificationContent> GetContent(Notification notification, Channel channel)
    {
        var content = new UNMutableNotificationContent
        {
            Title = notification.Title!,
            Body = notification.Message!
        };
        if (!notification.LocalAttachmentPath.IsEmpty())
        {
            var imageUri = NSUrl.FromString(notification.LocalAttachmentPath!);
            var attachment = UNNotificationAttachment.FromIdentifier("image", imageUri, new UNNotificationAttachmentOptions(), out var _);
            content.Attachments = new [] { attachment };
        }
        if (!notification.Thread.IsEmpty())
            content.ThreadIdentifier = notification.Thread!;

        if (notification.BadgeCount != null)
            content.Badge = notification.BadgeCount.Value;

        if (notification.Payload?.Any() ?? false)
            content.UserInfo = notification.Payload!.ToNsDictionary();

        await this.ApplyChannel(notification, channel, content);
        return content;
    }


    protected virtual async Task ApplyChannel(Notification notification, Channel channel, UNMutableNotificationContent native)
    {
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
#if IOS
                    var is12 = OperatingSystem.IsIOSVersionAtLeast(12);
#elif MACCATALYST
                    var is12 = OperatingSystem.IsMacOSVersionAtLeast(12);
#endif

                    native.Sound = this.configuration.UNAuthorizationOptions.HasFlag(UNAuthorizationOptions.CriticalAlert) && is12
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
#if IOS
            var geo = notification.Geofence!;

            trigger = UNLocationNotificationTrigger.CreateTrigger(
                new CLRegion(
                    new CLLocationCoordinate2D(geo.Center!.Latitude, geo.Center!.Longitude),
                    geo.Radius!.TotalMeters,
                    notification.Id.ToString()
                ),
                geo.Repeat
            );
#endif
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


    public async void OnDidReceiveNotificationResponse(UNNotificationResponse response, Action completionHandler)
    {
        var t = response.Notification?.Request?.Trigger;

        if (t == null || t is not UNPushNotificationTrigger)
        {
            var shiny = response.FromNative();
            await this.delegates
                .Value
                .RunDelegates(x => x.OnEntry(shiny))
                .ConfigureAwait(false);
        }
    }


    public void OnWillPresentNotification(UNNotification notification, Action<UNNotificationPresentationOptions> completionHandler) { }
    public void Handle(NSDictionary options)
    {
        if (options.ContainsKey(UIApplication.LaunchOptionsLocalNotificationKey))
        {
            var data = options[UIApplication.LaunchOptionsRemoteNotificationKey] as NSDictionary;
            if (data != null)
            {
                // TODO: need to parse this back into a notification
                //data.FromNsDictionary();
            }
        }
    }
}
