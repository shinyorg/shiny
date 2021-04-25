Title: Ranging
---
Ranging is a foreground only operation.  You can scan for "all" beacons with a particular filter set.  Meaning if you only filter by UUID, all beacons under that UUID will be returned.  With ranging, all of the identifier values are provided.  You are 


|Area|Info|
|----|----|
|NuGet| [![BeaconsNugetShield]][BeaconsNuget] |
|Shiny Startup|services.UseBeaconRanging()|
|Main Service|Shiny.Beacons.IBeaconMonitoringManager|
|Background Delegate|none|
|Static Generated|ShinyBeaconRanging|
|Manual Resolve|ShinyHost.Resolve<Shiny.Beacons.IBeaconRangingManager>()|
|Xamarin.Forms|DependencyService.Get<Shiny.Beacons.IBeaconRangingManager>()|

```cs
using System;
using Shiny;
using Shiny.Beacons;

public class BeaconRanging
{
    public void MyMethod()
    {
        // note, major & minor are optional here, but if you supply minor, you must supply major
        var ranging = ShinyHost.Resolve<IBeaconRangingManager>();
        ranging
            .WhenBeaconRanged(new BeaconRegion(
                "YourId",
                Guid.Parse(""),
                1, // major
                1 // minor
            ))
            .Subscribe(beacon =>
            {
                // you will get the full details of the beacon seen heree
            });
    }
}


```