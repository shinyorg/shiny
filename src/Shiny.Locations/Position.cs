using System;

namespace Shiny.Locations;


/// <summary>
/// A geographic point expressed as a latitude/longitude pair. Validates that both values fall within their valid ranges.
/// </summary>
/// <param name="Latitude">The latitude in decimal degrees, between -90 and 90.</param>
/// <param name="Longitude">The longitude in decimal degrees, between -180 and 180.</param>
public record Position(double Latitude, double Longitude)
{
    // forces validation to run after primary ctor
    readonly bool valid = Check.Assert(Latitude, Longitude);

    internal static class Check
    {
        internal static bool Assert(double latitude, double longitude)
        {
            if (latitude < -90 || latitude > 90)
                throw new ArgumentException($"Invalid latitude value - {latitude}");

            if (longitude < -180 || longitude > 180)
                throw new ArgumentException($"Invalid longitude value - {longitude}");

            return true;
        }
    }


    /// <summary>
    /// Computes the great-circle distance between this position and another.
    /// </summary>
    /// <param name="other">The other position.</param>
    /// <returns>The distance between the two points.</returns>
    public Distance GetDistanceTo(Position other) => Distance.Between(this, other);


    //https://stackoverflow.com/questions/2042599/direction-between-2-latitude-longitude-points-in-c-sharp
    /// <summary>
    /// Computes the compass bearing in degrees from this position toward another.
    /// </summary>
    /// <param name="to">The destination position.</param>
    /// <returns>The bearing in degrees from 0 to 360.</returns>
    public double GetCompassBearingTo(Position to)
    {
        var dLon = ToRad(to.Longitude - this.Longitude);
        var dPhi = Math.Log(Math.Tan(ToRad(to.Latitude) / 2 + Math.PI / 4) / Math.Tan(ToRad(this.Latitude) / 2 + Math.PI / 4));
        if (Math.Abs(dLon) > Math.PI)
            dLon = dLon > 0 ? -(2 * Math.PI - dLon) : (2 * Math.PI + dLon);

        return ToBearing(Math.Atan2(dLon, dPhi));
    }


    /// <summary>
    /// Converts a value from degrees to radians.
    /// </summary>
    /// <param name="degrees">The angle in degrees.</param>
    /// <returns>The equivalent angle in radians.</returns>
    public static double ToRad(double degrees)
        => degrees * (Math.PI / 180);


    /// <summary>
    /// Converts a value from radians to degrees.
    /// </summary>
    /// <param name="radians">The angle in radians.</param>
    /// <returns>The equivalent angle in degrees.</returns>
    public static double ToDegrees(double radians)
        => radians * 180 / Math.PI;


    /// <summary>
    /// Normalizes a radian value to a compass bearing in the range 0 to 360 degrees.
    /// </summary>
    /// <param name="radians">The angle in radians.</param>
    /// <returns>The bearing in degrees.</returns>
    public static double ToBearing(double radians)
        => (ToDegrees(radians) + 360) % 360;
}