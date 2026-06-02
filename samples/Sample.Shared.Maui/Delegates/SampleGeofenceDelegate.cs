using Microsoft.Extensions.Logging;
using Sample.Shared.Maui.Services;
using Shiny.Notifications;

namespace Sample.Shared.Maui.Delegates;

public class SampleGeofenceDelegate(
    ILogger<SampleGeofenceDelegate> logger,
    INotificationManager notificationManager,
    IEventStore events
) : IGeofenceDelegate
{
    public async Task OnStatusChanged(GeofenceState newStatus, GeofenceRegion region)
    {
        var msg = $"Geofence status: {newStatus}, region: {region.Identifier}";
        logger.LogInformation(msg);

        await events.Add(
            "Geofence",
            msg,
            new Dictionary<string, string?>
            {
                ["Identifier"] = region.Identifier,
                ["Status"] = newStatus.ToString(),
                ["Latitude"] = region.Center.Latitude.ToString("F6"),
                ["Longitude"] = region.Center.Longitude.ToString("F6"),
                ["RadiusMeters"] = region.Radius.TotalMeters.ToString("F1")
            }
        );
        await notificationManager.Send("Shiny Geofence", msg);
    }
}
