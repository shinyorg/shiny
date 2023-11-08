using Shiny.Locations;

namespace Sample.Gps;


public partial class MyGpsDelegate : GpsDelegate
{
    readonly SampleSqliteConnection conn;
    public MyGpsDelegate(ILogger<MyGpsDelegate> logger, SampleSqliteConnection conn) : base(logger)
    {
        this.conn = conn;
        this.MinimumDistance = Distance.FromMeters(10);
        this.MinimumTime = TimeSpan.FromSeconds(10);
    }

    protected override Task OnGpsReading(GpsReading reading) => this.conn.Log(
        "GPS",
        $"{reading.Position.Latitude} / {reading.Position.Longitude} - H: {reading.Heading}",
        $"Accuracy: {reading.PositionAccuracy} - SP: {reading.Speed}",
        reading.Timestamp
    );
}
//public partial class MyGpsDelegate : IGpsDelegate
//{
//    readonly SampleSqliteConnection conn;
//    public MyGpsDelegate(SampleSqliteConnection conn) => this.conn = conn;

//    public Task OnReading(GpsReading reading) => this.conn.Log(
//        "GPS",
//        $"{reading.Position.Latitude} / {reading.Position.Longitude} - H: {reading.Heading}",
//        $"Accuracy: {reading.PositionAccuracy} - SP: {reading.Speed}",
//        reading.Timestamp
//    );
//}

#if ANDROID
public partial class MyGpsDelegate : IAndroidForegroundServiceDelegate
{
    public void Configure(AndroidX.Core.App.NotificationCompat.Builder builder)
    {
        
    }
}
#endif