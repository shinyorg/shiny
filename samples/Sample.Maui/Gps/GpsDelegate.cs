using System;
using System.Threading.Tasks;
using Shiny.Locations;


namespace Sample
{
    public class GpsDelegate : IGpsDelegate
    {
        readonly SampleSqliteConnection conn;
        public GpsDelegate(SampleSqliteConnection conn) => this.conn = conn;


        public Task OnReading(IGpsReading reading) => this.conn.InsertAsync(new ShinyEvent
        {
            Text = $"{reading.Position.Latitude} / {reading.Position.Longitude} - H: {reading.Heading}",
            Detail = $"Acc: {reading.PositionAccuracy} - SP: {reading.Speed}",
            Timestamp = reading.Timestamp.ToLocalTime()
        });
    }
}
