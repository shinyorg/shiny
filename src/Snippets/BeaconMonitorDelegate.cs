using System.Threading.Tasks;
using Shiny.Beacons;


public class BeaconMonitorDelegate : IBeaconMonitorDelegate
{
    public async Task OnStatusChanged(BeaconRegionState newStatus, BeaconRegion region)
    {
        // NOTE: you cannot not see the actual detected beacon here, only the region that was crossed
        // this is done by the OS to protect privacy of the user
    }
}