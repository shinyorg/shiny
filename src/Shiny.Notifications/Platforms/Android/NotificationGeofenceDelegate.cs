using System.Threading.Tasks;

using Shiny.Infrastructure;
using Shiny.Locations;


namespace Shiny.Notifications
{
    public class NotificationGeofenceDelegate : IGeofenceDelegate
    {
        const string KEY = "NOTIFICATION:";

        readonly INotificationManager notificationManager;
        readonly IGeofenceManager geofenceManager;
        readonly IRepository repository;


        public NotificationGeofenceDelegate(INotificationManager notificationManager, IGeofenceManager geofenceManager, IRepository repository)
        { 
            this.notificationManager = notificationManager;
            this.geofenceManager = geofenceManager;
            this.repository = repository;
        }


        public async Task OnStatusChanged(GeofenceState newStatus, GeofenceRegion region)
        {
            if (newStatus == GeofenceState.Entered && region.Identifier.StartsWith(KEY))
            {
                var notificationId = region.Identifier.Replace(KEY, "");
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


        public static string GetGeofenceId(Notification notification) => KEY + notification.Id.ToString();
    }
}
