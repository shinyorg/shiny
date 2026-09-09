namespace Shiny.Beacons.Infrastructure;


/// <summary>
/// Turns a signal strength reading into an estimated distance in metres.
/// </summary>
/// <remarks>
/// Implementations are handed <b>filtered</b> RSSI, never a single raw advertisement - see
/// <see cref="RssiFilter"/>. Register your own through
/// <see cref="Shiny.Beacons.BeaconRangingOptions.DistanceEstimator"/> when neither shipped model
/// matches the hardware you are deploying against.
/// </remarks>
public interface IBeaconDistanceEstimator
{
    /// <summary>
    /// Estimates the distance to a transmitter.
    /// </summary>
    /// <param name="rssi">The filtered signal strength in dBm.</param>
    /// <param name="txPower">The transmitter's calibrated power at one metre, in dBm.</param>
    /// <returns>The estimated distance in metres, or a negative value when it cannot be estimated.</returns>
    double Estimate(double rssi, sbyte txPower);
}
