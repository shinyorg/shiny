using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.OS;
using Microsoft.Extensions.Logging;
using Shiny.Hosting;
using Shiny.Locations;
using Shiny.Stores;
using Shiny.Support.Repositories;
using P = Android.Manifest.Permission;

namespace Shiny.Notifications;


public partial class NotificationManager : INotificationManager,
                                           IAndroidLifecycle.IOnActivityOnCreate,
                                           IAndroidLifecycle.IOnActivityNewIntent
{
    readonly Lazy<AndroidNotificationProcessor> processor;
    readonly AndroidPlatform platform;
    readonly AndroidNotificationManager manager;
    readonly IChannelManager channelManager;
    readonly IRepository repository;
    readonly IGeofenceManager geofenceManager;
    readonly IKeyValueStore settings;
    readonly ILogger logger;


    public NotificationManager(
        IServiceProvider services,
        AndroidPlatform platform,
        AndroidNotificationManager manager,
        IRepository repository,
        IChannelManager channelManager,
        IGeofenceManager geofenceManager,
        IKeyValueStoreFactory keystore,
        ILogger<NotificationManager> logger
    )
    {
        this.processor = services.GetLazyService<AndroidNotificationProcessor>();
        this.platform = platform;
        this.manager = manager;
        this.repository = repository;
        this.channelManager = channelManager;
        this.geofenceManager = geofenceManager;
        this.settings = keystore.DefaultStore;
        this.logger = logger;
    }


    public void AddChannel(Channel channel) => this.channelManager.Add(channel);
    public void RemoveChannel(string channelId) => this.channelManager.Remove(channelId);
    public void ClearChannels() => this.channelManager.Clear();
    public Channel? GetChannel(string channelId) => this.channelManager.Get(channelId);
    public IReadOnlyList<Channel> GetChannels() => this.channelManager.GetAll();


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
            var notifications = this.repository.GetList<AndroidNotification>();
            foreach (var notification in notifications)
            {
                await this.CancelInternal(notification).ConfigureAwait(false);
            }
            this.repository.Clear<AndroidNotification>();
            
        }
    }


    public Task<Notification?> GetNotification(int notificationId)
        => Task.FromResult((Notification?)this.repository.Get<AndroidNotification>(notificationId.ToString()));


    public Task<IReadOnlyList<Notification>> GetPendingNotifications()
        => Task.FromResult((IReadOnlyList<Notification>)this.repository.GetList<AndroidNotification>().OfType<Notification>().ToList());


    /* - ANDROID 14 - https://developer.android.com/about/versions/14/changes/schedule-exact-alarms
val alarmManager: AlarmManager = context.getSystemService<AlarmManager>()!!
when {
   // If permission is granted, proceed with scheduling exact alarms.
   alarmManager.canScheduleExactAlarms() -> {
       alarmManager.setExact(...)
   }
   else -> {
       // Ask users to go to exact alarm page in system settings.
       startActivity(Intent(ACTION_REQUEST_SCHEDULE_EXACT_ALARM))
   }
}

    override fun onResume() {
   …  
   if (alarmManager.canScheduleExactAlarms()) {
       // Set exact alarms.
       alarmManager.setExact(...)
   }
   else {
       // Permission not yet approved. Display user notice and revert to a fallback  
       // approach.
       alarmManager.setWindow(...)
   }
}


    AlarmManager.ACTION_SCHEDULE_EXACT_ALARM_PERMISSION_STATE_CHANGED
     */
    //public Task<NotificationAccessState> RequestAccess(AccessRequestFlags flags)
    //{

    //}
    public Task<NotificationAccessState> GetCurrentAccess()
    {
        using var alarm = this.platform.GetSystemService<AlarmManager>(Context.AlarmService);
        var notificationStatus = this.platform.GetCurrentPermissionStatus(P.PostNotifications);

        var status = new NotificationAccessState(
            notificationStatus,
            this.geofenceManager.CurrentStatus,
            alarm!.CanScheduleExactAlarms() ? AccessState.Available : AccessState.Restricted
        );
        return Task.FromResult(status);
    }


    public async Task<AccessState> RequestAccess(AccessRequestFlags access)
    {
        var list = new List<string>();
        if (OperatingSystem.IsAndroidVersionAtLeast(33))
            list.Add(P.PostNotifications); // required

        if (access.HasFlag(AccessRequestFlags.TimeSensitivity) && !OperatingSystem.IsAndroidVersionAtLeast(32))
        {
            // if denied, restricted
            if (OperatingSystem.IsAndroidVersionAtLeast(31))
                list.Add(P.ScheduleExactAlarm);
        }
             
        if (access.HasFlag(AccessRequestFlags.LocationAware))
            list.AddRange(new[] { P.AccessCoarseLocation, P.AccessFineLocation }); // required, along with access bg
        
        var result = await this.platform.RequestPermissions(list.ToArray()).ToTask();
        
        if (list.Contains(P.PostNotifications) && !result.IsGranted(P.PostNotifications))
            return AccessState.Denied;

        if (access.HasFlag(AccessRequestFlags.LocationAware))
        {
            if (!result.IsGranted(P.AccessFineLocation))
                return AccessState.Denied;

            if (OperatingSystem.IsAndroidVersionAtLeast(29))
            {
                var bgResult = await this.platform.RequestAccess(P.AccessBackgroundLocation).ToTask();
                if (bgResult != AccessState.Available)
                    return AccessState.Denied;
            }
        }

        if (access.HasFlag(AccessRequestFlags.TimeSensitivity) && OperatingSystem.IsAndroidVersionAtLeast(32))
        {
            using var alarm = this.platform.GetSystemService<AlarmManager>(Context.AlarmService);
            if (!alarm.CanScheduleExactAlarms())
            {
                var tcs = new TaskCompletionSource();
                using var _ = this.platform
                    .WhenActivityStatusChanged()
                    .Where(x => x.State == ActivityState.Resumed)
                    .Take(1)
                    .Subscribe(_ =>
                    {
                        tcs.SetResult();
                    });

                // TODO: INotificationManager.Send will requestaccess and this will fail if running in a background job
                    // TODO: notification will need a current state like other modules
                //const SettingsKeyValueStore = ACTION_REQUEST_SCHEDULE_EXACT_ALARM
                this.platform.CurrentActivity!.StartActivity(new Intent("android.settings.REQUEST_SCHEDULE_EXACT_ALARM"));
                await tcs.Task.ConfigureAwait(false);

                if (!alarm.CanScheduleExactAlarms())
                    return AccessState.Restricted;
            }
        }

        if (list.Contains(P.ScheduleExactAlarm) && !result.IsGranted(P.ScheduleExactAlarm))
            return AccessState.Restricted;

        if (!this.manager.NativeManager.AreNotificationsEnabled())
            return AccessState.Disabled;

        return AccessState.Available;
    }


    public async Task Send(Notification notification)
    {
        notification.AssertValid();
        var androidNotification = notification.TryToNative<AndroidNotification>();

        // TODO: should I cancel an existing id if the user is setting it?
        if (androidNotification.Id == 0)
            androidNotification.Id = this.settings.IncrementValue("NotificationId");

        var channelId = androidNotification.Channel ?? Channel.Default.Identifier;
        var channel = this.channelManager.Get(channelId);
        if (channel == null)
            throw new InvalidProgramException("No channel found for " + channelId);

        var builder = this.manager.CreateNativeBuilder(androidNotification, channel!);

        if (androidNotification.Geofence != null)
        {
            await this.geofenceManager.StartMonitoring(new GeofenceRegion(
                AndroidNotificationProcessor.GetGeofenceId(androidNotification),
                androidNotification.Geofence!.Center!,
                androidNotification.Geofence!.Radius!
            ));
        }
        else if (androidNotification.RepeatInterval != null)
        {
            // calc first date if repeating interval
            androidNotification.ScheduleDate = androidNotification.RepeatInterval!.CalculateNextAlarm();
        }

        if (androidNotification.ScheduleDate == null && androidNotification.Geofence == null)
        {
            var native = builder.Build();
            this.manager.NativeManager.Notify(androidNotification.Id, native);
        }
        else
        {
            // ensure a channel is set
            androidNotification.Channel = channel!.Identifier;
            this.repository.Set(androidNotification);

            if (androidNotification.ScheduleDate != null)
                this.manager.SetAlarm(androidNotification);
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
    Task<NotificationAccessState> INotificationManager.RequestAccess(AccessRequestFlags flags) => throw new NotImplementedException();
}
