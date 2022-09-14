using System;
using System.Threading.Tasks;
using Shiny.Locations;
using Shiny.Notifications;


namespace Sample
{
    public class GeofenceDelegate : IGeofenceDelegate
    {
        readonly INotificationManager notificationManager;
        readonly SampleSqliteConnection conn;


        public GeofenceDelegate(INotificationManager notificationManager, SampleSqliteConnection conn)
        {
            this.notificationManager = notificationManager;
            this.conn = conn;
        }


        public async Task OnStatusChanged(GeofenceState newStatus, GeofenceRegion region)
        {
            var state = newStatus.ToString().ToUpper();

            await this.conn.InsertAsync(new ShinyEvent
            {
                Text = region.Identifier,
                Detail = $"You {state} the geofence",
                Timestamp = DateTime.Now
            });
            await this.notificationManager.Send(
                "Geofencing",
                $"You {state} the geofence"
            );
        }
    }
}
