Title: Monitoring
---

Beacon monitoring is quite a bit different than ranging.  Monitoring does not provide distance and has many limitations, but it works in the background.  Monitoring uses beacon regions much like ranging, however, the region is all that is returned in the delegate to protect user privacy
Monitoring is limited to a maximum of 20 regions on iOS.  On Android & UWP, there currently isn't a limit HOWEVER the BLE scans do emit quite a number of scan packets.  If you have too many, your scan will slow down your app quite a bit.

<?! PackageInfo "Shiny.Beacons" "Shiny.Beacons.IBeaconMonitorManager" /?>

## Gotcha
* Scanning for any beacons always works down to specific filter sets meaning you are adding (not mixing & matching)
    1. UUID
    2. Major
    3. Minor
* You must always have an initial filter of a UUID for ranging or monitoring
* iOS limits the amount of beacons you are allowed to monitor to 20.  This value is also shared with your geofences, so you need to think about your strategy when using beacons


```cs
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
```