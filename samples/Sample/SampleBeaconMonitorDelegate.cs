using Shiny.Beacons;

namespace Sample;


public class SampleBeaconMonitorDelegate : IBeaconMonitorDelegate
{
    public Task OnStatusChanged(BeaconRegionState newStatus, BeaconRegion region) => Task.CompletedTask;
}
