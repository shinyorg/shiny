using System;

namespace Shiny.Beacons.Infrastructure;


/// <summary>
/// The log-distance path loss model: <c>d = 10 ^ ((txPower - rssi) / (10 * n))</c>.
/// </summary>
/// <param name="PathLossExponent">
/// The environmental attenuation factor <c>n</c>. 2.0 is free space; raise it towards 3-4 for
/// cluttered indoor environments where the signal decays faster than the inverse square law.
/// </param>
/// <remarks>
/// This is the default estimator. Unlike the curve fit it has no hardware-specific magic numbers,
/// so it behaves identically on every platform and degrades predictably as the environment changes.
/// </remarks>
public record PathLossDistanceEstimator(double PathLossExponent = 2.0) : IBeaconDistanceEstimator
{
    /// <inheritdoc />
    public double Estimate(double rssi, sbyte txPower)
    {
        // A zero RSSI means the platform had no measurement to give us, not a very strong signal.
        if (rssi == 0)
            return -1;

        if (this.PathLossExponent <= 0)
            throw new InvalidOperationException("The path loss exponent must be greater than zero");

        return Math.Pow(10d, (txPower - rssi) / (10d * this.PathLossExponent));
    }
}
