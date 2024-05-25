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


public class AndroidNotificationProcessor(
    AndroidNotificationManager notificationManager,
    IGeofenceManager geofenceManager,
    IRepository repository,
    ISerializer serializer,
    ILogger<AndroidNotificationProcessor> logger,
    IEnumerable<INotificationDelegate> delegates
)
{
    public const string IntentNotificationKey = "ShinyNotification";
    public const string IntentActionKey = "Action";
    public const string RemoteInputResultKey = "Result";


    public static string GetGeofenceId(Notification notification) => GeofenceKey + notification.Id.ToString();
    const string GeofenceKey = "NOTIFICATION:";


    public async Task TryProcessIntent(Intent? intent)
    {
        if (intent == null || !delegates.Any())
            return;

        if (intent.HasExtra(IntentNotificationKey))
        {
            var notificationString = intent.GetStringExtra(IntentNotificationKey);
            var notification = serializer.Deserialize<Notification>(notificationString);

            var action = intent.GetStringExtra(IntentActionKey);
            var text = RemoteInput.GetResultsFromIntent(intent)?.GetString("Result");
            var response = new NotificationResponse(notification, action, text);

            // the notification lives within the intent since it has already been removed from the repo
            await delegates
                .RunDelegates(
                    x => x.OnEntry(response),
                    logger
                )
                .ConfigureAwait(false);

            notificationManager.NativeManager.Cancel(notification.Id);
        }
    }


    public void ProcessPending()
    {
        // fire any missed/pending alarms?
        var missed = repository.GetList<AndroidNotification>(
            x => x.ScheduleDate != null &&
            x.ScheduleDate < DateTime.UtcNow
        );

        foreach (var notification in missed)
        {
            notificationManager.Send(notification);
            this.DeleteOrReschedule(notification);
        }
    }


    public async Task ProcessGeofence(GeofenceState newStatus, GeofenceRegion region)
    {
        // this is to match iOS behaviour
        if (newStatus != GeofenceState.Entered || !region.Identifier.StartsWith(GeofenceKey))
            return;

        var notificationId = region.Identifier.Replace(GeofenceKey, String.Empty);
        var notification = repository.Get<AndroidNotification>(notificationId);

        if (notification?.Geofence != null)
        {
            notificationManager.Send(notification);

            if (!notification.Geofence.Repeat)
            {
                repository.Remove<AndroidNotification>(notificationId);
                await geofenceManager
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
            var notification = repository.Get<AndroidNotification>(id.ToString());
            if (notification != null)
            {
                notificationManager.Send(notification);
                this.DeleteOrReschedule(notification);
            }
        }
    }


    void DeleteOrReschedule(Notification notification)
    {
        if (notification.RepeatInterval == null)
        {
            repository.Remove<AndroidNotification>(notification.Id.ToString());
        }
        else
        {
            // if repeating, set next time
            notification.ScheduleDate = notification.RepeatInterval.CalculateNextAlarm();
            repository.Set(notification);

            notificationManager.SetAlarm(notification);
        }
    }
}
