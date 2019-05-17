using System;
using CoreLocation;


namespace Shiny.Beacons
{
    public static class Extensions
    {
        public static CLBeaconRegion ToNative(this BeaconRegion region)
        {
            if (region.Uuid == null)
                throw new ArgumentException("You must pass a UUID for the Beacon Region");

            var uuid = region.Uuid.ToNSUuid();
            CLBeaconRegion native = null;

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


        public static Proximity FromNative(this CLProximity proximity)
        {
            switch (proximity)
            {
                case CLProximity.Far:
                    return Proximity.Far;

                case CLProximity.Immediate:
                    return Proximity.Immediate;

                case CLProximity.Near:
                    return Proximity.Near;

                case CLProximity.Unknown:
                default:
                    return Proximity.Unknown;
            }
        }


        public static CLProximity ToNative(this Proximity proximity)
        {
            switch (proximity)
            {
                case Proximity.Far:
                    return CLProximity.Far;

                case Proximity.Immediate:
                    return CLProximity.Immediate;

                case Proximity.Near:
                    return CLProximity.Near;

                case Proximity.Unknown:
                default:
                    return CLProximity.Unknown;
            }
        }
    }
}
