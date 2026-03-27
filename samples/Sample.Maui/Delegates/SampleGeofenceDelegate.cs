using Microsoft.Extensions.Logging;
using Shiny.Locations;

namespace Sample.Maui.Delegates;

public class SampleGeofenceDelegate(ILogger<SampleGeofenceDelegate> logger) : IGeofenceDelegate
{
    public Task OnStatusChanged(GeofenceState newStatus, GeofenceRegion region)
    {
        logger.LogInformation("Geofence {Identifier}: {Status}", region.Identifier, newStatus);
        return Task.CompletedTask;
    }
}
