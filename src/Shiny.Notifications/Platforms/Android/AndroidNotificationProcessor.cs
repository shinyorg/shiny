using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Microsoft.Extensions.Logging;
using Shiny.Locations;
using Shiny.Stores;
using Shiny.Support.Repositories;

namespace Shiny.Notifications;


public class AndroidNotificationProcessor
{
    public const string IntentNotificationKey = "ShinyNotification";
    public const string IntentActionKey = "Action";
    public const string RemoteInputResultKey = "Result";

    readonly AndroidNotificationManager notificationManager;
    readonly IGeofenceManager geofenceManager;
    readonly IRepository repository;
    readonly ISerializer serializer;
    readonly ILogger logger;
    readonly IEnumerable<INotificationDelegate> delegates;


    public AndroidNotificationProcessor(
        AndroidNotificationManager notificationManager,
        IGeofenceManager geofenceManager,
        IRepository repository,
        ISerializer serializer,
        ILogger<AndroidNotificationProcessor> logger,
        IEnumerable<INotificationDelegate> delegates
    )
    {
        this.notificationManager = notificationManager;
        this.geofenceManager = geofenceManager;
        this.repository = repository;
        this.serializer = serializer;
        this.logger = logger;
        this.delegates = delegates;
    }


    public static string GetGeofenceId(Notification notification) => GeofenceKey + notification.Id.ToString();
    const string GeofenceKey = "NOTIFICATION:";


    public async Task TryProcessIntent(Intent? intent)
    {
        if (intent == null || !this.delegates.Any())
            return;

        if (intent.HasExtra(IntentNotificationKey))
        {
            var notificationString = intent.GetStringExtra(IntentNotificationKey);
            var notification = this.serializer.Deserialize<Notification>(notificationString);

            var action = intent.GetStringExtra(IntentActionKey);
            var text = RemoteInput.GetResultsFromIntent(intent)?.GetString("Result");
            var response = new NotificationResponse(notification, action, text);

            // the notification lives within the intent since it has already been removed from the repo
            await this.delegates
                .RunDelegates(
                    x => x.OnEntry(response),
                    this.logger
                )
                .ConfigureAwait(false);

            this.notificationManager.NativeManager.Cancel(notification.Id);
        }
    }


    public void ProcessPending()
    {
        // fire any missed/pending alarms?
        var missed = this.repository.GetList<AndroidNotification>(
            x => x.ScheduleDate != null &&
            x.ScheduleDate < DateTime.UtcNow
        );

        foreach (var notification in missed)
        {
            this.notificationManager.Send(notification);
            this.DeleteOrReschedule(notification);
        }
    }


    public async Task ProcessGeofence(GeofenceState newStatus, GeofenceRegion region)
    {
        // this is to match iOS behaviour
        if (newStatus != GeofenceState.Entered || !region.Identifier.StartsWith(GeofenceKey))
            return;

        var notificationId = region.Identifier.Replace(GeofenceKey, String.Empty);
        var notification = this.repository.Get<AndroidNotification>(notificationId);

        if (notification?.Geofence != null)
        {
            this.notificationManager.Send(notification);

            if (!notification.Geofence.Repeat)
            {
                this.repository.Remove<AndroidNotification>(notificationId);
                await this.geofenceManager
                    .StopMonitoring(region.Identifier)
                    .ConfigureAwait(false);
            }
        }
    }


    public void ProcessAlarm(Intent intent)
    {
        // get notification for alarm
        var id = intent.GetIntExtra(IntentNotificationKey, 0);
        if (id > 0)
        {
            var notification = this.repository.Get<AndroidNotification>(id.ToString());
            if (notification != null)
            {
                this.notificationManager.Send(notification);
                this.DeleteOrReschedule(notification);
            }
        }
    }


    void DeleteOrReschedule(Notification notification)
    {
        if (notification.RepeatInterval == null)
        {
            this.repository.Remove<AndroidNotification>(notification.Id.ToString());
        }
        else
        {
            // if repeating, set next time
            notification.ScheduleDate = notification.RepeatInterval.CalculateNextAlarm();
            this.repository.Set(notification);

            this.notificationManager.SetAlarm(notification);
        }
    }
}
