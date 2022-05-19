using System.Threading.Tasks;

namespace Shiny.Beacons;


public interface IBeaconMonitorDelegate
{
    Task OnStatusChanged(BeaconRegionState newStatus, BeaconRegion region);
}
