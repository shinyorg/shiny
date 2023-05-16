using System;
using CoreLocation;
using Foundation;

namespace Shiny.Beacons;


public static class PlatformExtensions
{
    static NSUuid ToNsUuid(BeaconRegion region)
    {
        if (region.Uuid == Guid.Empty)
            throw new ArgumentException("You must pass a UUID for the Beacon Region");

        var uuid = region.Uuid.ToNSUuid();
        return uuid;
    }


    public static CLBeaconRegion ToNative(this BeaconRegion region)
    {
        var uuid = ToNsUuid(region);
        CLBeaconRegion native;

        if (region.Major > 0 && region.Minor > 0)
            native = new CLBeaconRegion(uuid, region.Major.Value, region.Minor.Value, region.Identifier);

        else if (region.Major > 0)
            native = new CLBeaconRegion(uuid, region.Major.Value, region.Identifier);

        else
            native = new CLBeaconRegion(uuid, region.Identifier);

        //native.NotifyEntryStateOnDisplay = true;
        native.NotifyOnEntry = region.NotifyOnEntry;
        native.NotifyOnExit = region.NotifyOnExit;

        return native;
    }


    public static CLBeaconIdentityConstraint ToCLBeaconIdentityConstraint(this BeaconRegion region)
    {
        var uuid = ToNsUuid(region);

        if (region.Major > 0 && region.Minor > 0)
            return new CLBeaconIdentityConstraint(uuid, region.Major.Value, region.Minor.Value);

        if (region.Major > 0)
            return new CLBeaconIdentityConstraint(uuid, region.Major.Value);

        return new CLBeaconIdentityConstraint(uuid);
    }


    public static Proximity FromNative(this CLProximity proximity) => proximity switch 
    {
        CLProximity.Far => Proximity.Far,
        CLProximity.Immediate => Proximity.Immediate,
        CLProximity.Near => Proximity.Near,
        _ => Proximity.Unknown
    };

    
    // public static CLProximity ToNative(this Proximity proximity) => proximity switch
    // {
    //     Proximity.Far => CLProximity.Far,
    //     Proximity.Immediate => CLProximity.Immediate,
    //     Proximity.Near => CLProximity.Near,
    //     _ => CLProximity.Unknown
    // };
}
