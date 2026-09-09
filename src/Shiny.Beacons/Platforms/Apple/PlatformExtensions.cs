using System;
using CoreLocation;
using Foundation;

namespace Shiny.Beacons;


/// <summary>
/// Conversions between Shiny's beacon types and CoreLocation's.
/// </summary>
public static class PlatformExtensions
{
    /// <summary>
    /// Builds the CoreLocation constraint used for ranging.
    /// </summary>
    /// <param name="region">The region to convert.</param>
    public static CLBeaconIdentityConstraint ToConstraint(this BeaconRegion region)
    {
        var uuid = region.Uuid.ToNSUuid();

        if (region.Major == null)
            return new CLBeaconIdentityConstraint(uuid);

        return region.Minor == null
            ? new CLBeaconIdentityConstraint(uuid, region.Major.Value)
            : new CLBeaconIdentityConstraint(uuid, region.Major.Value, region.Minor.Value);
    }


#if !MACOS
    /// <summary>
    /// Builds the CoreLocation condition used by CLMonitor on iOS and Mac Catalyst 18+.
    /// </summary>
    /// <param name="region">The region to convert.</param>
    public static CLBeaconIdentityCondition ToCondition(this BeaconRegion region)
    {
        var uuid = region.Uuid.ToNSUuid();

        if (region.Major == null)
            return new CLBeaconIdentityCondition(uuid);

        return region.Minor == null
            ? new CLBeaconIdentityCondition(uuid, region.Major.Value)
            : new CLBeaconIdentityCondition(uuid, region.Major.Value, region.Minor.Value);
    }


    /// <summary>
    /// Builds the CLBeaconRegion used by the pre-18 CLLocationManager monitoring API.
    /// </summary>
    /// <param name="region">The region to convert.</param>
    public static CLBeaconRegion ToNative(this BeaconRegion region)
    {
        var uuid = region.Uuid.ToNSUuid();

        CLBeaconRegion native;
        if (region.Major == null)
            native = new CLBeaconRegion(uuid, region.Identifier);
        else if (region.Minor == null)
            native = new CLBeaconRegion(uuid, region.Major.Value, region.Identifier);
        else
            native = new CLBeaconRegion(uuid, region.Major.Value, region.Minor.Value, region.Identifier);

        native.NotifyOnEntry = region.NotifyOnEntry;
        native.NotifyOnExit = region.NotifyOnExit;

        return native;
    }
#endif


    /// <summary>
    /// Converts a CoreLocation observation into a <see cref="Beacon"/>.
    /// </summary>
    /// <param name="native">The CoreLocation beacon.</param>
    /// <param name="options">Supplies the proximity thresholds and the clock.</param>
    /// <remarks>
    /// CoreLocation hands back a distance it has already smoothed, so no filtering or path loss
    /// model is applied here - doing so would fight the OS. TxPower is null for the same reason:
    /// the raw advertisement never reaches the app on Apple platforms.
    /// </remarks>
    public static Beacon FromNative(this CLBeacon native, BeaconRangingOptions options)
    {
        // CoreLocation reports -1 when it cannot estimate, which lines up with our own convention
        var distance = native.Accuracy;

        return new Beacon(
            native.Uuid.ToGuid(),
            native.Major.UInt16Value,
            native.Minor.UInt16Value,
            (int)native.Rssi,
            native.Proximity.FromNative(distance, options),
            distance,
            null,
            options.TimeProvider.GetUtcNow()
        );
    }


    /// <summary>
    /// Maps CoreLocation's proximity onto Shiny's, falling back to the configured distance
    /// thresholds when CoreLocation has not classified the reading.
    /// </summary>
    /// <param name="proximity">CoreLocation's classification.</param>
    /// <param name="distance">The estimated distance in metres.</param>
    /// <param name="options">Supplies the thresholds used for the fallback.</param>
    public static Proximity FromNative(this CLProximity proximity, double distance, BeaconRangingOptions options)
        => proximity switch
        {
            CLProximity.Immediate => Proximity.Immediate,
            CLProximity.Near => Proximity.Near,
            CLProximity.Far => Proximity.Far,
            _ => options.ToProximity(distance)
        };
}
