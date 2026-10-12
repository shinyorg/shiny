using Microsoft.Extensions.Logging;
using Sample.Shared.Maui.Services;
using Shiny.Notifications;

namespace Sample.Shared.Maui.Delegates;

public class SampleRegionGeofenceDelegate(
    ILogger<SampleRegionGeofenceDelegate> logger,
    INotificationManager notificationManager,
    IEventStore events
) : IRegionGeofenceDelegate
{
    public async Task OnRegionChanged(RegionChange change)
    {
        var verb = change.Entered ? "Entered" : "Exited";
        var msg = $"{verb} {change.RegionName ?? change.RegionId} ({change.RegionSet})";
        logger.LogInformation(msg);

        await events.Add(
            "Region Geofence",
            msg,
            new Dictionary<string, string?>
            {
                ["RegionSet"] = change.RegionSet,
                ["RegionId"] = change.RegionId,
                ["RegionName"] = change.RegionName,
                ["Transition"] = verb,
                ["Latitude"] = change.Position.Latitude.ToString("F6"),
                ["Longitude"] = change.Position.Longitude.ToString("F6")
            }
        );
        await notificationManager.Send("Shiny Region Geofence", msg);
    }
}
