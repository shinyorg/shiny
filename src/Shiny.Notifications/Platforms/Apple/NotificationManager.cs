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
using Microsoft.Extensions.Logging;

namespace Shiny.Notifications;


public class NotificationManager : INotificationManager, IIosLifecycle.INotificationHandler
{
    readonly Lazy<IEnumerable<INotificationDelegate>> delegates;
    readonly IosConfiguration configuration;
    readonly IPlatform platform;
    readonly ILogger logger;
    readonly IChannelManager channelManager;
    readonly IKeyValueStore settings;


    public NotificationManager(
        IosConfiguration configuration,
        IServiceProvider services,
        ILogger<NotificationManager> logger,
        IPlatform platform,
        IChannelManager channelManager,
        IKeyValueStoreFactory keystore
    )
    {
        this.configuration = configuration;
        this.delegates = services.GetLazyService<IEnumerable<INotificationDelegate>>();
        this.logger = logger;
        this.platform = platform;
        this.channelManager = channelManager;
        this.settings = keystore.DefaultStore;
    }


    public void AddChannel(Channel channel) => this.channelManager.Add(channel);
    public void RemoveChannel(string channelId) => this.channelManager.Remove(channelId);
    public void ClearChannels() => this.channelManager.Clear();
    public Channel? GetChannel(string channelId) => this.channelManager.Get(channelId);
    public IList<Channel> GetChannels() => this.channelManager.GetAll();


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


    public Task<IList<Notification>> GetPendingNotifications() => this.platform.InvokeTaskOnMainThread(async () =>
    {
        var requests = await UNUserNotificationCenter
            .Current
            .GetPendingNotificationRequestsAsync()
            .ConfigureAwait(false);

        var notifications = requests.Select(x => x.FromNative() as Notification).ToList();
        return (IList<Notification>)notifications;
    });


    public Task Send(Notification notification) => this.platform.InvokeTaskOnMainThread(async () =>
    {
        notification.AssertValid();

        if (notification.Id == 0)
            notification.Id = this.settings.IncrementValue("NotificationId");

        var channel = Channel.Default;
        if (!notification.Channel.IsEmpty())
        {
            channel = this.channelManager.Get(notification.Channel!);
            if (channel == null)
                throw new InvalidOperationException($"{notification.Channel} does not exist");
        }

        var content = this.GetContent(notification, channel);

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


    protected virtual UNMutableNotificationContent GetContent(Notification notification, Channel channel)
    {
        var content = new UNMutableNotificationContent
        {
            Title = notification.Title!,
            Body = notification.Message!
        };

        if (!notification.Thread.IsEmpty())
            content.ThreadIdentifier = notification.Thread!;

        if (notification.BadgeCount != null)
            content.Badge = notification.BadgeCount.Value;

        if (notification.Payload?.Any() ?? false)
            content.UserInfo = notification.Payload!.ToNsDictionary();

        if (notification is AppleNotification apple)
        {
            content.TargetContentIdentifier = apple.TargetContentIdentifier;
            if (apple.Subtitle != null)
                content.Subtitle = apple.Subtitle;
            
            if (OperatingSystemShim.IsAppleVersionAtleast(16))
            {
                content.FilterCriteria = apple.FilterCriteria;
                content.RelevanceScore = apple.RelevanceScore;
            }

            if (apple.Attachments != null)
                content.Attachments = apple.Attachments;
        }
        this.ApplyChannel(notification, channel, content);
        return content;
    }


    protected virtual void ApplyChannel(Notification notification, Channel channel, UNMutableNotificationContent native)
    {
        if (OperatingSystemShim.IsAppleVersionAtleast(15))
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
        var useCriticalSound =
            this.configuration.UNAuthorizationOptions.HasFlag(UNAuthorizationOptions.CriticalAlert) &&
            channel.Importance == ChannelImportance.Critical &&
            UIDevice.CurrentDevice.CheckSystemVersion(12, 0);

        switch (channel.Sound)
        {
            case ChannelSound.High:
                native.Sound = useCriticalSound
                    ? UNNotificationSound.DefaultCriticalSound
                    : UNNotificationSound.Default;
                break;

            case ChannelSound.Default:
                native.Sound = UNNotificationSound.Default;
                break;

            case ChannelSound.Custom:
                native.Sound = useCriticalSound
                    ? UNNotificationSound.GetCriticalSound(channel.CustomSoundPath!)
                    : UNNotificationSound.GetSound(channel.CustomSoundPath!);
                break;

            case ChannelSound.None:
                break;
        }

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
                    var is12 = OperatingSystemShim.IsAppleVersionAtleast(12);
                    native.Sound = this.configuration.UNAuthorizationOptions.HasFlag(UNAuthorizationOptions.CriticalAlert) && is12
                        ? UNNotificationSound.DefaultCriticalSound
                        : UNNotificationSound.Default;
                    break;

                case ChannelImportance.Normal:
                    OperatingSystemShim.IsMacCatalystVersionAtLeast(12);
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
                .RunDelegates(x => x.OnEntry(shiny), this.logger)
                .ConfigureAwait(false);

            this.platform.InvokeOnMainThread(() =>
                completionHandler.Invoke()
            );
        }
    }


    public void OnWillPresentNotification(UNNotification notification, Action<UNNotificationPresentationOptions> completionHandler)
    {
        var t = notification?.Request?.Trigger;
        if (t == null || t is not UNPushNotificationTrigger)
        {
            this.platform.InvokeOnMainThread(() =>
                completionHandler.Invoke(this.configuration.PresentationOptions)
            );
        }
    }
}