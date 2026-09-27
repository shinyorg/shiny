using System;
using Shiny.Locations;

namespace Shiny;


/// <summary>
/// A distance value stored in kilometers, with conversions to miles and meters and the ability to compute great-circle distances between positions.
/// </summary>
/// <param name="TotalKilometers">The distance in kilometers.</param>
public record Distance(double TotalKilometers)
{
    /// <summary>
    /// Conversion factor from miles to kilometers.
    /// </summary>
    public const double MILES_TO_KM = 1.60934;

    /// <summary>
    /// Conversion factor from kilometers to miles.
    /// </summary>
    public const double KM_TO_MILES = 0.621371;

    /// <summary>
    /// Conversion factor from kilometers to meters.
    /// </summary>
    public const int KM_TO_METERS = 1000;

    /// <summary>
    /// The distance expressed in miles.
    /// </summary>
    public double TotalMiles => this.TotalKilometers * KM_TO_MILES;

    /// <summary>
    /// The distance expressed in meters.
    /// </summary>
    public double TotalMeters => this.TotalKilometers * KM_TO_METERS;


    /// <summary>
    /// Computes the great-circle distance between two geographic positions.
    /// </summary>
    /// <param name="one">The first position.</param>
    /// <param name="two">The second position.</param>
    /// <returns>The distance between the two positions.</returns>
    public static Distance Between(Position one, Position two)
    {
        var d1 = one.Latitude * (Math.PI / 180.0);
        var num1 = one.Longitude * (Math.PI / 180.0);
        var d2 = two.Latitude * (Math.PI / 180.0);
        var num2 = two.Longitude * (Math.PI / 180.0) - num1;
        var d3 = Math.Pow(Math.Sin((d2 - d1) / 2.0), 2.0) +
                 Math.Cos(d1) * Math.Cos(d2) * Math.Pow(Math.Sin(num2 / 2.0), 2.0);

        var meters = 6376500.0 * (2.0 * Math.Atan2(Math.Sqrt(d3), Math.Sqrt(1.0 - d3)));
        return Distance.FromMeters(meters);
    }


    /// <inheritdoc/>
    public override string ToString() => $"[Distance: {this.TotalKilometers} km]";

    /// <summary>
    /// Returns true if the left distance is greater than the right.
    /// </summary>
    public static bool operator >(Distance x, Distance y) => x.TotalKilometers > y.TotalKilometers;

    /// <summary>
    /// Returns true if the left distance is less than the right.
    /// </summary>
    public static bool operator <(Distance x, Distance y) => x.TotalKilometers < y.TotalKilometers;

    /// <summary>
    /// Returns true if the left distance is greater than or equal to the right.
    /// </summary>
    public static bool operator >=(Distance x, Distance y) => x.TotalKilometers >= y.TotalKilometers;

    /// <summary>
    /// Returns true if the left distance is less than or equal to the right.
    /// </summary>
    public static bool operator <=(Distance x, Distance y) => x.TotalKilometers <= y.TotalKilometers;


    /// <summary>
    /// Creates a distance from a value expressed in whole miles.
    /// </summary>
    /// <param name="miles">The distance in miles.</param>
    public static Distance FromMiles(int miles) => new Distance(miles * MILES_TO_KM);

    /// <summary>
    /// Creates a distance from a value expressed in miles.
    /// </summary>
    /// <param name="miles">The distance in miles.</param>
    public static Distance FromMiles(double miles) => new Distance(miles * MILES_TO_KM);

    /// <summary>
    /// Creates a distance from a value expressed in meters.
    /// </summary>
    /// <param name="meters">The distance in meters.</param>
    public static Distance FromMeters(double meters) => new Distance(meters / KM_TO_METERS);

    /// <summary>
    /// Creates a distance from a value expressed in kilometers.
    /// </summary>
    /// <param name="km">The distance in kilometers.</param>
    public static Distance FromKilometers(double km) => new Distance(km);
}
