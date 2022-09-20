using Shiny.Locations;
using Shiny.Notifications;

namespace Sample.Geofencing;


public class MyGeofenceDelegate : IGeofenceDelegate
{
    readonly INotificationManager notificationManager;
    readonly SampleSqliteConnection conn;


    public MyGeofenceDelegate(INotificationManager notificationManager, SampleSqliteConnection conn)
    {
        this.notificationManager = notificationManager;
        this.conn = conn;
    }


    public async Task OnStatusChanged(GeofenceState newStatus, GeofenceRegion region)
    {
        var state = newStatus.ToString().ToUpper();

        await this.conn.Log(
            "Geofencing",
            "State Change",
            $"You {state} the geofence {region.Identifier}" 
        );
        await this.notificationManager.Send(
            "Geofencing",
            $"You {state} the geofence"
        );
    }
}
