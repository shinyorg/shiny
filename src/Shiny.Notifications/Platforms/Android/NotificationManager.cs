using System;
using System.Collections.Generic;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;
using Android;
using Android.Content;

using AndroidX.Core.App;

using Shiny.Hosting;
using Shiny.Locations;
using Shiny.Stores;

namespace Shiny.Notifications;


public partial class NotificationManager : INotificationManager, IAndroidLifecycle.IOnActivityNewIntent
{
    readonly Lazy<AndroidNotificationProcessor> processor;
    readonly AndroidPlatform platform;
    readonly IChannelManager channelManager;
    readonly AndroidNotificationManager manager;
    readonly IRepository<Notification> repository;
    readonly IGeofenceManager geofenceManager;
    readonly IKeyValueStore settings;


    public NotificationManager(
        IServiceProvider services,
        AndroidPlatform platform,
        AndroidNotificationManager manager,
        IRepository<Notification> repository,
        IChannelManager channelManager,
        IGeofenceManager geofenceManager,
        IKeyValueStoreFactory keystore
    )
    {
        this.processor = services.GetLazyService<AndroidNotificationProcessor>();
        this.platform = platform;
        this.manager = manager;
        this.repository = repository;
        this.channelManager = channelManager;
        this.geofenceManager = geofenceManager;
        this.settings = keystore.DefaultStore;
    }


    public Task AddChannel(Channel channel) => this.channelManager.Add(channel);
    public Task RemoveChannel(string channelId) => this.DeleteChannel(this.channelManager, channelId);
    public Task ClearChannels() => this.DeleteAllChannels(this.channelManager);
    public Task<IList<Channel>> GetChannels() => this.channelManager.GetAll();


    public async Task Cancel(int id)
    {
        var notification = await this.repository.Get(id.ToString()).ConfigureAwait(false);
        if (notification != null)
        {
            await this.CancelInternal(notification).ConfigureAwait(false);
            await this.repository.Remove(id.ToString()).ConfigureAwait(false);
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
            var notifications = await this.repository.GetList();
            foreach (var notification in notifications)
            {
                await this.CancelInternal(notification).ConfigureAwait(false);
            }
            await this.repository
                .Clear()
                .ConfigureAwait(false);
        }
    }


    public Task<Notification?> GetNotification(int notificationId)
        => this.repository.Get(notificationId.ToString());


    public async Task<IEnumerable<Notification>> GetPendingNotifications()
        => await this.repository.GetList().ConfigureAwait(false);


    public async Task<AccessState> RequestAccess(AccessRequestFlags access)
    {
        if (!this.manager.NativeManager.AreNotificationsEnabled())
            return AccessState.Disabled;

        if (access.HasFlag(AccessRequestFlags.LocationAware))
        {
            var locPermission = await this.geofenceManager.RequestAccess();
            if (locPermission != AccessState.Available)
                return AccessState.Restricted;
        }
        if (access.HasFlag(AccessRequestFlags.TimeSensitivity) && this.platform.IsMinApiLevel(31))
        {
            var schedulePermission = await this.platform
                .RequestPermissions(Manifest.Permission.ScheduleExactAlarm)
                .ToTask();

            if (!schedulePermission.IsSuccess())
                return AccessState.Restricted;
        }

        return AccessState.Available;
    }


    public async Task Send(Notification notification)
    {
        notification.AssertValid();

        // TODO: should I cancel an existing id if the user is setting it?
        if (notification.Id == 0)
            notification.Id = this.settings.IncrementValue("NotificationId");

        var channel = await this.channelManager.Get(notification.Channel ?? Channel.Default.Identifier);
        if (channel == null)
            throw new InvalidProgramException("There is no default channel!!");

        var builder = await this.manager
            .CreateNativeBuilder(notification, channel!)
            .ConfigureAwait(false);

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
            this.manager.SendNative(notification.Id, builder.Build());
        }
        else
        {
            // ensure a channel is set
            notification.Channel = channel!.Identifier;
            await this.repository.Set(notification).ConfigureAwait(false);

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

        }
    }
}
