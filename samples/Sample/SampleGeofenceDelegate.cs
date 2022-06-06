using Shiny.Locations;

namespace Sample;


public class SampleGeofenceDelegate : IGeofenceDelegate
{
    public Task OnStatusChanged(GeofenceState newStatus, GeofenceRegion region) => Task.CompletedTask;
}
