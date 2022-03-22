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

        readonly INotificationManager notificationManager;
        readonly IGeofenceManager geofenceManager;
        readonly IRepository repository;
        readonly ISerializer serializer;
        readonly IEnumerable<INotificationDelegate> delegates;


        public AndroidNotificationProcessor(INotificationManager notificationManager,
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
            // TODO: fire anything pending that missed alarms?
            // TODO: if repeating, set next time
        }


        public async Task ProcessGeofence(GeofenceState newStatus, GeofenceRegion region)
        {
            if (newStatus == GeofenceState.Entered && region.Identifier.StartsWith(GeofenceKey))
            {
                var notificationId = region.Identifier.Replace(GeofenceKey, "");
                var notification = await this.repository.Get<Notification>(notificationId).ConfigureAwait(false);

                if (notification?.Geofence != null)
                {
                    var repeat = notification.Geofence.Repeat;

                    notification.Geofence = null; // HACK
                    await this.notificationManager.Send(notification).ConfigureAwait(false);
                    if (!repeat)
                    {
                        await this.repository.Remove<Notification>(notificationId).ConfigureAwait(false);
                        await this.geofenceManager.StopMonitoring(region.Identifier).ConfigureAwait(false);
                    }
                }
            }
        }


        public async Task ProcessAlarm(Intent intent)
        {
            // TODO: get notification for alarm
            // TODO: nullify schedule date to send now

            // TODO: if repeating, set next time
        }
    }
}
