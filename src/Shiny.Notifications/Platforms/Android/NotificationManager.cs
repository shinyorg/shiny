using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Android.Content;
using Android.OS;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shiny.Hosting;
using Shiny.Locations;
using Shiny.Extensions.Stores;
using Shiny.Extensions.Stores.Repositories;
using P = Android.Manifest.Permission;

namespace Shiny.Notifications;


public partial class NotificationManager(
    IServiceProvider services,
    AndroidPlatform platform,
    AndroidNotificationManager manager,
    IRepository repository,
    IChannelManager channelManager,
    IGeofenceManager geofenceManager,
    [FromKeyedServices(StoreKeys.Default)] IKeyValueStore settings,
    ILogger<NotificationManager> logger
) : INotificationManager, IAndroidLifecycle.IOnActivityOnCreate, IAndroidLifecycle.IOnActivityNewIntent
{
    readonly Lazy<AndroidNotificationProcessor> processor = new(() => services.GetRequiredService<AndroidNotificationProcessor>());
    readonly AndroidPlatform platform = platform;
    readonly AndroidNotificationManager manager = manager;
    readonly IRepository repository = repository;
    readonly IChannelManager channelManager = channelManager;
    readonly IGeofenceManager geofenceManager = geofenceManager;
    readonly IKeyValueStore settings = settings;
    readonly ILogger logger = logger;


    public void AddChannel(Channel channel) => this.channelManager.Add(channel);
    public void RemoveChannel(string channelId) => this.channelManager.Remove(channelId);
    public void ClearChannels() => this.channelManager.Clear();
    public Channel? GetChannel(string channelId) => this.channelManager.Get(channelId);
    public IList<Channel> GetChannels() => this.channelManager.GetAll();


    public async Task Cancel(int id)
    {
        var notification = this.repository.Get<AndroidNotification>(id.ToString());
        if (notification != null)
        {
            await this.CancelInternal(notification).ConfigureAwait(false);
            this.repository.Remove<AndroidNotification>(id.ToString());
        }
    }


    public async Task Cancel(CancelScope scope = CancelScope.All)
    {
        if (scope == CancelScope.All || scope == CancelScope.DisplayedOnly)
        {
            this.manager.NativeManager.CancelAll();
        }
        if (scope == CancelScope.All || scope == CancelScope.Pending)
        {
            var notifications = this.repository.GetAll<AndroidNotification>();
            foreach (var notification in notifications)
            {
                await this.CancelInternal(notification).ConfigureAwait(false);
            }
            this.repository.Clear<AndroidNotification>();
        }
    }


    public Task<Notification?> GetNotification(int notificationId)
        => Task.FromResult((Notification?)this.repository.Get<AndroidNotification>(notificationId.ToString()));


    public Task<IList<Notification>> GetPendingNotifications()
        => Task.FromResult((IList<Notification>)this.repository.GetAll<AndroidNotification>().OfType<Notification>().ToList());


    public Task<AccessState> GetCurrentAccess(AccessRequestFlags flags = AccessRequestFlags.Notification)
    {
        if (OperatingSystem.IsAndroidVersionAtLeast(33))
        {
            var status = this.platform.GetCurrentPermissionStatus(P.PostNotifications);
            if (status != AccessState.Available)
                return Task.FromResult(status);
        }

        if (!this.manager.NativeManager.AreNotificationsEnabled())
            return Task.FromResult(AccessState.Disabled);

        if (flags.HasFlag(AccessRequestFlags.LocationAware))
        {
            var fineLocation = this.platform.GetCurrentPermissionStatus(P.AccessFineLocation);
            if (fineLocation != AccessState.Available)
                return Task.FromResult(AccessState.Denied);

            if (OperatingSystem.IsAndroidVersionAtLeast(29))
            {
                var bgLocation = this.platform.GetCurrentPermissionStatus(P.AccessBackgroundLocation);
                if (bgLocation != AccessState.Available)
                    return Task.FromResult(AccessState.Denied);
            }
        }

        if (OperatingSystem.IsAndroidVersionAtLeast(31) && flags.HasFlag(AccessRequestFlags.TimeSensitivity))
        {
            if (!this.manager.Alarms.CanScheduleExactAlarms())
                return Task.FromResult(AccessState.Restricted);
        }

        return Task.FromResult(AccessState.Available);
    }


    public async Task<AccessState> RequestAccess(AccessRequestFlags access)
    {
        var list = new List<string>();

        if (OperatingSystem.IsAndroidVersionAtLeast(33))
            list.Add(P.PostNotifications); // required

        if (access.HasFlag(AccessRequestFlags.LocationAware))
            list.AddRange(new[] { P.AccessCoarseLocation, P.AccessFineLocation }); // required, along with access bg

        if (list.Count > 0)
        {
            var result = await this.platform.RequestPermissions(list.ToArray());
            if (list.Contains(P.PostNotifications) && !result.IsGranted(P.PostNotifications))
                return AccessState.Denied;

            if (access.HasFlag(AccessRequestFlags.LocationAware))
            {
                if (!result.IsGranted(P.AccessFineLocation))
                    return AccessState.Denied;

                if (OperatingSystem.IsAndroidVersionAtLeast(29))
                {
                    var bgResult = await this.platform.RequestAccess(P.AccessBackgroundLocation);
                    if (bgResult != AccessState.Available)
                        return AccessState.Denied;
                }
            }
        }

        if (!this.manager.NativeManager.AreNotificationsEnabled())
            return AccessState.Disabled;

        // SCHEDULE_EXACT_ALARM is a special app-op permission, not a runtime permission
        // It must be checked via AlarmManager.CanScheduleExactAlarms() and granted via Settings
        if (OperatingSystem.IsAndroidVersionAtLeast(31) && access.HasFlag(AccessRequestFlags.TimeSensitivity))
        {
            if (!this.manager.Alarms.CanScheduleExactAlarms())
            {
                var intent = new Intent(
                    Android.Provider.Settings.ActionRequestScheduleExactAlarm,
                    Android.Net.Uri.Parse("package:" + this.platform.AppContext.PackageName)
                );
                intent.AddFlags(Android.Content.ActivityFlags.NewTask);
                this.platform.AppContext.StartActivity(intent);

                // wait for the user to return from settings
                await this.platform.WaitForActivity(ActivityState.Resumed).ConfigureAwait(false);

                if (!this.manager.Alarms.CanScheduleExactAlarms())
                    return AccessState.Restricted;
            }
        }

        return AccessState.Available;
    }


    public async Task Send(Notification notification)
    {
        notification.AssertValid();
        var android = notification.TryToNative<AndroidNotification>();

        // TODO: should I cancel an existing id if the user is setting it?
        if (notification.Id == 0)
            notification.Id = this.settings.IncrementValue("NotificationId");

        var channelId = notification.Channel ?? Channel.Default.Identifier;
        var channel = this.channelManager.Get(channelId);
        if (channel == null)
            throw new InvalidProgramException("No channel found for " + channelId);

        var builder = this.manager.CreateNativeBuilder(android, channel!);

        if (notification.Geofence != null)
        {
            await this.geofenceManager.StartMonitoring(new GeofenceRegion(
                AndroidNotificationProcessor.GetGeofenceId(notification),
                notification.Geofence!.Center!,
                notification.Geofence!.Radius!
            ));
        }
        else if (notification.RepeatInterval != null)
        {
            // calc first date if repeating interval
            notification.ScheduleDate = notification.RepeatInterval!.CalculateNextAlarm();
        }

        if (notification.ScheduleDate == null && notification.Geofence == null)
        {
            var native = builder.Build();
            this.manager.NativeManager.Notify(notification.Id, native);
        }
        else
        {
            // ensure a channel is set
            notification.Channel = channel!.Identifier;
            this.repository.Set(notification);

            if (notification.ScheduleDate != null)
                this.manager.SetAlarm(notification);
        }
    }


    protected virtual async Task CancelInternal(Notification notification)
    {
        if (notification.Geofence != null)
        {
            var geofenceId = AndroidNotificationProcessor.GetGeofenceId(notification);
            await this.geofenceManager.StopMonitoring(geofenceId);
        }
        if (notification.ScheduleDate != null || notification.RepeatInterval != null)
            this.manager.CancelAlarm(notification);

        this.manager.NativeManager.Cancel(notification.Id);
    }


    public async void Handle(Android.App.Activity activity, Intent intent)
    {
        try
        {
            await this.processor.Value
                .TryProcessIntent(intent)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Error trying to process intent");
        }
    }

    public void ActivityOnCreate(Android.App.Activity activity, Bundle? savedInstanceState)
        => this.Handle(activity, activity.Intent!);
}
