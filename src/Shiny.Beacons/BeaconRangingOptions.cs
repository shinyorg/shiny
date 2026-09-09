using System;
using Shiny.Beacons.Infrastructure;
using Shiny.BluetoothLE;

namespace Shiny.Beacons;


/// <summary>
/// Tunes how raw BLE advertisements are turned into distances and region transitions.
/// </summary>
/// <remarks>
/// These settings drive the shared scanning engine used on Android, Windows, Linux, Blazor and
/// plain .NET hosts. Apple platforms range iBeacons through CoreLocation, which does its own
/// filtering and hands back a distance directly, so only <see cref="RegionExitTimeout"/> and the
/// proximity thresholds apply there - and only for Eddystone.
/// </remarks>
public record BeaconRangingOptions
{
    /// <summary>
    /// The model used to convert a filtered RSSI into metres.
    /// Defaults to <see cref="PathLossDistanceEstimator"/> with an exponent of 2.0.
    /// </summary>
    public IBeaconDistanceEstimator DistanceEstimator { get; init; } = new PathLossDistanceEstimator();

    /// <summary>
    /// How far back RSSI samples are kept when averaging. Defaults to 20 seconds.
    /// Shorter windows react faster to movement; longer ones read more steadily when stationary.
    /// </summary>
    public TimeSpan RssiFilterWindow { get; init; } = TimeSpan.FromSeconds(20);

    /// <summary>
    /// The fraction of samples discarded from each end of the sorted window before averaging.
    /// Defaults to 0.1, so the strongest and weakest tenth are both dropped.
    /// </summary>
    public double RssiTrimFraction { get; init; } = 0.1;

    /// <summary>
    /// The calibrated power assumed when a beacon advertises none. Defaults to -59 dBm.
    /// </summary>
    public sbyte DefaultTxPower { get; init; } = IBeaconPacket.DefaultTxPower;

    /// <summary>
    /// Distances below this are reported as <see cref="Proximity.Immediate"/>. Defaults to 0.5 m.
    /// </summary>
    public double ImmediateThreshold { get; init; } = 0.5;

    /// <summary>
    /// Distances below this - and at or above <see cref="ImmediateThreshold"/> - are reported as
    /// <see cref="Proximity.Near"/>. Everything beyond is <see cref="Proximity.Far"/>. Defaults to 3 m.
    /// </summary>
    public double NearThreshold { get; init; } = 3.0;

    /// <summary>
    /// How long a monitored region goes without a matching advertisement before it is treated as
    /// exited. Defaults to 30 seconds.
    /// </summary>
    /// <remarks>
    /// This has to absorb the gaps a beacon's own advertising interval and a throttled background
    /// scan introduce. Too short and a stationary device flaps in and out of the region; too long
    /// and exit events lag badly. 30 seconds tolerates the common 1 Hz beacon interval comfortably.
    /// </remarks>
    public TimeSpan RegionExitTimeout { get; init; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// How often the monitoring engine re-evaluates region state to look for exits.
    /// Defaults to 5 seconds.
    /// </summary>
    public TimeSpan RegionEvaluationInterval { get; init; } = TimeSpan.FromSeconds(5);

    /// <summary>
    /// When true, an advertisement whose payload looks like an iBeacon is accepted even if it did
    /// not arrive under Apple's company identifier. Defaults to false.
    /// </summary>
    /// <remarks>
    /// A handful of vendors ship iBeacon-shaped payloads under their own Bluetooth SIG identifier.
    /// Turning this on will also let genuinely unrelated manufacturer data through if it happens to
    /// start with the bytes 0x02 0x15, so it is off unless you know you need it.
    /// </remarks>
    public bool AllowNonAppleCompanyId { get; init; } = false;

    /// <summary>
    /// The clock the filters and region timers run against. Defaults to the system clock; tests
    /// substitute a fake so windows and timeouts can be driven deterministically.
    /// </summary>
    public TimeProvider TimeProvider { get; init; } = TimeProvider.System;


    /// <summary>
    /// Maps an estimated distance onto a <see cref="Proximity"/> bucket using the configured thresholds.
    /// </summary>
    /// <param name="distance">The estimated distance in metres. Negative values are unknown.</param>
    public Proximity ToProximity(double distance)
    {
        if (distance < 0 || Double.IsNaN(distance))
            return Proximity.Unknown;

        if (distance < this.ImmediateThreshold)
            return Proximity.Immediate;

        return distance < this.NearThreshold
            ? Proximity.Near
            : Proximity.Far;
    }
}
