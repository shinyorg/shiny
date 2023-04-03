using System;

namespace Shiny.Locations;


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


    public Distance GetDistanceTo(Position other) => Distance.Between(this, other);


    //https://stackoverflow.com/questions/2042599/direction-between-2-latitude-longitude-points-in-c-sharp
    public double GetCompassBearingTo(Position to)
    {
        var dLon = ToRad(to.Longitude - this.Longitude);
        var dPhi = Math.Log(Math.Tan(ToRad(to.Latitude) / 2 + Math.PI / 4) / Math.Tan(ToRad(this.Latitude) / 2 + Math.PI / 4));
        if (Math.Abs(dLon) > Math.PI)
            dLon = dLon > 0 ? -(2 * Math.PI - dLon) : (2 * Math.PI + dLon);

        return ToBearing(Math.Atan2(dLon, dPhi));
    }


    public static double ToRad(double degrees)
        => degrees * (Math.PI / 180);


    public static double ToDegrees(double radians)
        => radians * 180 / Math.PI;


    public static double ToBearing(double radians)
        => (ToDegrees(radians) + 360) % 360;
}