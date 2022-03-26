using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Shiny.Infrastructure;
using Shiny.Locations;


namespace Shiny.Notifications
{
    public class AndroidNotificationProcessor
    {
        public const string IntentNotificationKey = "ShinyNotification";
        public const string IntentActionKey = "Action";
        public const string RemoteInputResultKey = "Result";

        readonly AndroidNotificationManager notificationManager;
        readonly IGeofenceManager geofenceManager;
        readonly IRepository repository;
        readonly ISerializer serializer;
        readonly IEnumerable<INotificationDelegate> delegates;


        public AndroidNotificationProcessor(AndroidNotificationManager notificationManager,
                                            IGeofenceManager geofenceManager, 
                                            IRepository repository,
                                            ISerializer serializer, 
                                            IEnumerable<INotificationDelegate> delegates)
        {
            this.notificationManager = notificationManager;
            this.geofenceManager = geofenceManager;
            this.repository = repository;
            this.serializer = serializer;
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
                await this.delegates.RunDelegates(x => x.OnEntry(response)).ConfigureAwait(false);
            }
        }


        public async Task ProcessPending()
        {
            // fire any missed/pending alarms?
            var missed = await this.repository
                .GetList<Notification>(x => x.ScheduleDate != null && x.ScheduleDate < DateTime.UtcNow)
                .ConfigureAwait(false);

            foreach (var notification in missed)
            {
                await this.notificationManager.Send(notification).ConfigureAwait(false);
                await this.DeleteOrReschedule(notification).ConfigureAwait(false);
            }
        }


        public async Task ProcessGeofence(GeofenceState newStatus, GeofenceRegion region)
        {
            // this is to match iOS behaviour
            if (newStatus != GeofenceState.Entered || !region.Identifier.StartsWith(GeofenceKey))
                return;

            var notificationId = region.Identifier.Replace(GeofenceKey, String.Empty);
            var notification = await this.repository.Get<Notification>(notificationId).ConfigureAwait(false);

            if (notification?.Geofence != null)
            {
                await this.notificationManager.Send(notification).ConfigureAwait(false);

                if (!notification.Geofence.Repeat)
                {
                    await this.repository.Remove<Notification>(notificationId).ConfigureAwait(false);
                    await this.geofenceManager.StopMonitoring(region.Identifier).ConfigureAwait(false);
                }
            }
        }


        public async Task ProcessAlarm(Intent intent)
        {
            // get notification for alarm
            var id = intent.GetIntExtra(IntentNotificationKey, 0);
            if (id > 0)
            {
                var notification = await this.repository.Get<Notification>(id.ToString()).ConfigureAwait(false);
                if (notification != null)
                { 
                    await this.notificationManager.Send(notification).ConfigureAwait(false);
                    await this.DeleteOrReschedule(notification).ConfigureAwait(false);
                }
            }
        }


        async Task DeleteOrReschedule(Notification notification)
        {
            if (notification.RepeatInterval == null)
            {
                await this.repository
                    .Remove<Notification>(notification.Id.ToString())
                    .ConfigureAwait(false);
            }
            else
            {
                // if repeating, set next time
                notification.ScheduleDate = notification.RepeatInterval.CalculateNextAlarm();
                await this.repository.Set(notification.Id.ToString(), notification).ConfigureAwait(false);

                this.notificationManager.SetAlarm(notification);
            }
        }
    }
}
