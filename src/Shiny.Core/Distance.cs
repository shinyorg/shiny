using System;

namespace Shiny
{
    public sealed class Distance : IEquatable<Distance>
    {
        public const double MILES_TO_KM = 1.60934;
        public const double KM_TO_MILES = 0.621371;
        public const int KM_TO_METERS = 1000;


        public double TotalMiles => this.TotalKilometers * KM_TO_MILES;
        public double TotalMeters => this.TotalKilometers * KM_TO_METERS;
        public double TotalKilometers { get; set; }


        public override string ToString() => $"[Distance: {this.TotalKilometers} km]";
        public bool Equals(Distance other) => (this.TotalKilometers) == (other?.TotalKilometers);
        public override bool Equals(object obj) => obj is Distance distance && this.Equals(distance);
        public override int GetHashCode() => (this.TotalKilometers).GetHashCode();

        public static bool operator ==(Distance left, Distance right) => Equals(left, right);
        public static bool operator !=(Distance left, Distance right) => !Equals(left, right);
        public static bool operator >(Distance x, Distance y) => x.TotalKilometers > y.TotalKilometers;
        public static bool operator <(Distance x, Distance y) => x.TotalKilometers < y.TotalKilometers;
        public static bool operator >=(Distance x, Distance y) => x.TotalKilometers >= y.TotalKilometers;
        public static bool operator <=(Distance x, Distance y) => x.TotalKilometers <= y.TotalKilometers;


        public static Distance FromMiles(int miles) => new Distance
        {
            TotalKilometers = miles * MILES_TO_KM
        };
        public static Distance FromMiles(double miles) => new Distance
        {
            TotalKilometers = miles * MILES_TO_KM
        };
        public static Distance FromMeters(double meters) => new Distance
        {
            TotalKilometers = meters / KM_TO_METERS
        };
        public static Distance FromKilometers(double km) => new Distance
        {
            TotalKilometers = km
        };
    }
}
