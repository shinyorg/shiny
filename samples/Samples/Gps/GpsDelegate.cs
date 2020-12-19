using System;
using System.Threading.Tasks;
using Samples.Infrastructure;
using Samples.Models;
using Shiny.Locations;


namespace Samples.Gps
{
    public class GpsDelegate : IGpsDelegate
    {
        readonly CoreDelegateServices services;
        public GpsDelegate(CoreDelegateServices services) => this.services = services;


        public Task OnReading(IGpsReading reading)
            => this.services.Connection.InsertAsync(new GpsEvent
            {
                Latitude = reading.Position.Latitude,
                Longitude = reading.Position.Longitude,
                Altitude = reading.Altitude,
                PositionAccuracy = reading.PositionAccuracy,
                Heading = reading.Heading,
                HeadingAccuracy = reading.HeadingAccuracy,
                Speed = reading.Speed,
                Date = reading.Timestamp.ToLocalTime()
            });
    }
}
