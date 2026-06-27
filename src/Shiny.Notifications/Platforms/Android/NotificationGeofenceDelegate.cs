using System.Threading.Tasks;
using Shiny.Locations;

namespace Shiny.Notifications;


public class NotificationGeofenceDelegate(AndroidNotificationProcessor processor) : IGeofenceDelegate
{
    public Task OnStatusChanged(GeofenceState newStatus, GeofenceRegion region) => processor.ProcessGeofence(newStatus, region);
}
