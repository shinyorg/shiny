using Shiny.Locations;

namespace Sample.Gps;


public class MyGpsDelegate : IGpsDelegate
#if ANDROID
#endif
{
    readonly SampleSqliteConnection conn;
    public MyGpsDelegate(SampleSqliteConnection conn) => this.conn = conn;

    public Task OnReading(GpsReading reading) => this.conn.Log(
        "GPS",
        $"{reading.Position.Latitude} / {reading.Position.Longitude} - H: {reading.Heading}",
        $"Accuracy: {reading.PositionAccuracy} - SP: {reading.Speed}",
        reading.Timestamp
    );
}
