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

