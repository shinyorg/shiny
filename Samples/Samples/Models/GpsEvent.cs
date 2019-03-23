using System;
using SQLite;


namespace Samples.Models
{
    public class GpsEvent
    {
        [PrimaryKey]
        [AutoIncrement]
        public int Id { get; set; }

        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double PositionAccuracy { get; set; }
        public double Altitude { get; set; }
        public double Speed { get; set; }
        public double Heading { get; set; }
        public double HeadingAccuracy { get; set; }

        public DateTime Date { get; set; }
    }
}
