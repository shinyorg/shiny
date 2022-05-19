using System.Threading.Tasks;
using Shiny.Locations;

namespace Shiny.Notifications;


public class NotificationGeofenceDelegate : IGeofenceDelegate
{
    readonly AndroidNotificationProcessor processor;
    public NotificationGeofenceDelegate(AndroidNotificationProcessor processor) => this.processor = processor;
    public Task OnStatusChanged(GeofenceState newStatus, GeofenceRegion region) => this.processor.ProcessGeofence(newStatus, region);
}
