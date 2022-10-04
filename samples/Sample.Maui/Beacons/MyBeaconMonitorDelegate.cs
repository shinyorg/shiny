using Shiny.Beacons;

namespace Sample.Beacons;


public partial class MyBeaconMonitorDelegate : IBeaconMonitorDelegate
{
    readonly SampleSqliteConnection conn;
    public MyBeaconMonitorDelegate(SampleSqliteConnection conn) => this.conn = conn;
    public Task OnStatusChanged(BeaconRegionState newStatus, BeaconRegion region) => this.conn.Log(
        "Beacons",
        $"{region.Identifier} was {newStatus.ToString().ToLower()}",
        $"UUID: {region.Uuid} - M: {region.Major} - m: {region.Minor}"
    );
}

#if ANDROID
public partial class MyBeaconMonitorDelegate : IAndroidForegroundServiceDelegate
{
    public void Configure(AndroidX.Core.App.NotificationCompat.Builder builder)
    {

    }
}
#endif