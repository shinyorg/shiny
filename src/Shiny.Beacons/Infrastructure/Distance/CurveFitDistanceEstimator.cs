using System;

namespace Shiny.Beacons.Infrastructure;


/// <summary>
/// The empirical curve fit used across the Android beacon ecosystem, originally published by
/// Radius Networks and carried by the AltBeacon library.
/// </summary>
/// <remarks>
/// The constants were fitted against a Nexus 4 and a specific beacon, so accuracy varies with the
/// receiving hardware. It is shipped for parity with what Android developers see from other beacon
/// stacks; <see cref="PathLossDistanceEstimator"/> is the default because it is hardware-neutral.
/// </remarks>
public record CurveFitDistanceEstimator : IBeaconDistanceEstimator
{
    /// <inheritdoc />
    public double Estimate(double rssi, sbyte txPower)
    {
        if (rssi == 0)
            return -1;

        // txPower is negative, so this ratio grows as the signal weakens
        var ratio = rssi / (double)txPower;

        if (ratio < 1.0)
            return Math.Pow(ratio, 10);

        return (0.89976 * Math.Pow(ratio, 7.7095)) + 0.111;
    }
}
