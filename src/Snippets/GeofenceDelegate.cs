using System.Threading.Tasks;
using Shiny.Locations;

public class GeofenceDelegate : IGeofenceDelegate
{
    public async Task OnStatusChanged(GeofenceState newStatus, GeofenceRegion region)
    {
    }
}
