Title: Ranging
---
Ranging is a foreground only operation.  You can scan for "all" beacons with a particular filter set.  Meaning if you only filter by UUID, all beacons under that UUID will be returned.  With ranging, all of the identifier values are provided.  You are 


<?! PackageInfo "Shiny.Beacons" "Shiny.Beacons.IBeaconRangingManager" /?>

<?! Startup ?>
services.UseBeaconRanging();
<?!/ Startup ?>

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