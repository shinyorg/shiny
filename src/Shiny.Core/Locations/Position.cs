using System;


namespace Shiny.Locations
{
    public class Position : IEquatable<Position>
    {
        public Position(double lat, double lng)
        {
            if (lat < -90 || lat > 90)
                throw new ArgumentException($"Invalid latitude value - {lat}");

            if (lng < -180 || lng > 180)
                throw new ArgumentException($"Invalid longitude value - {lng}");

            this.Latitude = lat;
            this.Longitude = lng;
        }


        public double Latitude { get; }
        public double Longitude { get; }


        public Distance GetDistanceTo(Position other)
        {
            var d1 = this.Latitude * (Math.PI / 180.0);
            var num1 = this.Longitude * (Math.PI / 180.0);
            var d2 = other.Latitude * (Math.PI / 180.0);
            var num2 = other.Longitude * (Math.PI / 180.0) - num1;
            var d3 = Math.Pow(Math.Sin((d2 - d1) / 2.0), 2.0) +
                     Math.Cos(d1) * Math.Cos(d2) * Math.Pow(Math.Sin(num2 / 2.0), 2.0);

            var meters = 6376500.0 * (2.0 * Math.Atan2(Math.Sqrt(d3), Math.Sqrt(1.0 - d3)));
            return Distance.FromMeters(meters);
        }


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


        public override string ToString() => $"Latitude: {this.Latitude} - Longitude: {this.Longitude}";
        public bool Equals(Position? other) => other != null && (this.Latitude, this.Longitude).Equals((other.Latitude, other.Longitude));
        public static bool operator ==(Position? left, Position? right) => Equals(left, right);
        public static bool operator !=(Position? left, Position? right) => !Equals(left, right);
        public override bool Equals(object obj) => obj is Position pos && this.Equals(pos);
        public override int GetHashCode() => (this.Latitude, this.Longitude).GetHashCode();
    }
}
