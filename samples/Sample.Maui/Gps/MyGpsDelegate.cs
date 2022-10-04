using Shiny.Locations;

namespace Sample.Gps;


public partial class MyGpsDelegate : IGpsDelegate
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

#if ANDROID
public partial class MyGpsDelegate : IAndroidForegroundServiceDelegate
{
    public void Configure(AndroidX.Core.App.NotificationCompat.Builder builder)
    {
        
    }
}
#endif