using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Reactive.Linq;
using Foundation;
using UIKit;
using UserNotifications;
using CoreLocation;
using Microsoft.Extensions.Logging;
using Shiny.Hosting;
using Shiny.Stores;

namespace Shiny.Notifications;


public class NotificationManager : IAppleNotificationManager, IIosLifecycle.INotificationHandler
{
    readonly Lazy<IEnumerable<INotificationDelegate>> delegates;
    readonly IPlatform platform;
    readonly ILogger logger;
    readonly IChannelManager channelManager;
    readonly IKeyValueStore settings;


    public NotificationManager(
        IServiceProvider services,
        ILogger<NotificationManager> logger,
        IPlatform platform,
        IChannelManager channelManager,
        IKeyValueStoreFactory keystore
    )
    {
        this.delegates = services.GetLazyService<IEnumerable<INotificationDelegate>>();
        this.logger = logger;
        this.platform = platform;
        this.channelManager = channelManager;
        this.settings = keystore.DefaultStore;

        //this.PresentationOptions = UNNotificationPresentationOptions.Badge | UNNotificationPresentationOptions.Sound | UNNotificationPresentationOptions.Banner;
    }


    // TODO: persist
    //public UNNotificationPresentationOptions PresentationOptions { get; }

    public void AddChannel(Channel channel) => this.channelManager.Add(channel);
    public void AddChannel(AppleChannel channel) => this.AddChannel((Channel)channel);

    public void RemoveChannel(string channelId) => this.channelManager.Remove(channelId);
    public void ClearChannels() => this.channelManager.Clear();
    public Channel? GetChannel(string channelId) => this.channelManager.Get(channelId);
    public IReadOnlyList<Channel> GetChannels() => this.channelManager.GetAll();
    
    public Task<int> GetBadge() => this.platform.InvokeOnMainThreadAsync(() =>
#pragma warning disable CA1422
        (int)UIApplication.SharedApplication.ApplicationIconBadgeNumber
    );


    public Task SetBadge(int? badge) => this.platform.InvokeOnMainThreadAsync(async () =>
    {
        if (IosPlatform.IsAppleVersionAtleast(16))
        {
#pragma warning disable CA1416
            await UNUserNotificationCenter.Current.SetBadgeCountAsync(badge ?? 0);
        }
        else
        {
#pragma warning disable CA1422
            UIApplication.SharedApplication.ApplicationIconBadgeNumber = badge ?? 0;
        }
    });


    public async Task RequestAccess(UNAuthorizationOptions authOptions)
    {
        var tcs = new TaskCompletionSource<bool>();
        UNUserNotificationCenter.Current.RequestAuthorization(
            authOptions,
            (approved, error) =>
            {
                if (error != null)
                {
                    tcs.SetException(new Exception(error.Description));
                }
                else
                {
                    //var state = approved ? AccessState.Available : AccessState.Denied;
                    //tcs.SetResult(state);
                }
            });

        await tcs.Task.ConfigureAwait(false);
    }


    public async Task<NotificationAccessState> GetCurrentAccess()
    {
        // could track this to a persisted var on startup and update in RequestAccess

        // TODO: may need an AppleCurrentAccess
        var settings = await UNUserNotificationCenter.Current.GetNotificationSettingsAsync();
        //UNAuthorizationStatus.Ephemeral
        //UNAuthorizationStatus.Authorized
        //UNAuthorizationStatus.Denied
        //UNAuthorizationStatus.NotDetermined
        //UNAuthorizationStatus.Provisional

        //settings.AuthorizationStatus == UNAuthorizationStatus.Authorized
        //settings.AnnouncementSetting == UNNotificationSetting.
        //settings.AlertSetting == UNNotificationSetting
        //settings.CriticalAlertSetting
        //settings.CarPlaySetting
        //settings.LockScreenSetting
        //settings.ProvidesAppNotificationSettings
        //settings.ScheduledDeliverySetting == UNNotificationSetting.Enabled
        //settings.SoundSetting
        //settings.TimeSensitiveSetting
        //settings.BadgeSetting
        return null;
    }


    public async Task<NotificationAccessState> RequestAccess(AccessRequestFlags access)
    {
        // TODO: look at presentation options
        var nativeRequest = UNAuthorizationOptions.None;
        //if (access.HasFlag(AccessRequestFlags.LocationAware) || access.HasFlag(AccessRequestFlags.TimeSensitivity))
        //    nativeRequest |= UNAuthorizationOptions.TimeSensitive;

        await this.RequestAccess(nativeRequest);

        //return tcs.Task;
        return null;
    }


    public async Task<Notification?> GetNotification(int notificationId)
        => (await this.GetPendingNotifications()).FirstOrDefault(x => x.Id == notificationId);


    public Task<IReadOnlyList<Notification>> GetPendingNotifications() => this.platform.InvokeTaskOnMainThread(async () =>
    {
        var requests = await UNUserNotificationCenter
            .Current
            .GetPendingNotificationRequestsAsync()
            .ConfigureAwait(false);

        var notifications = requests.Select(x => x.FromNative() as Notification).ToList();
        return (IReadOnlyList<Notification>)notifications;
    });


    public Task Send(AppleNotification notification) => this.Send((Notification)notification);
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
            
            if (IosPlatform.IsAppleVersionAtleast(16))
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
        if (IosPlatform.IsAppleVersionAtleast(15))
        {
            native.InterruptionLevel = channel.Importance switch
            {
                ChannelImportance.Low => UNNotificationInterruptionLevel.Passive2,
                ChannelImportance.High => UNNotificationInterruptionLevel.TimeSensitive2,
                ChannelImportance.Critical => UNNotificationInterruptionLevel.Critical2,
                _ => UNNotificationInterruptionLevel.Active2
            };
        }

        // TODO
        native.CategoryIdentifier = channel.Identifier;
        var useCriticalSound =
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
                    native.Sound = UNNotificationSound.DefaultCriticalSound;
                    break;

                case ChannelImportance.High:
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
            //var options = this.delegates
            //    .Value
            //    .OfType<IAppleNotificationDelegate>()
            //    .Select(x => x.GetForegroundPresentation(notification))
            //    .FirstOrDefault(x => x != null);

            //this.platform.InvokeOnMainThread(() =>
            //    completionHandler.Invoke(options ?? PresentationOP)
            //);
        }
    }
}