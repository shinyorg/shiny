using Shiny.Beacons;

namespace Sample.Beacons;


public class MyBeaconMonitorDelegate : IBeaconMonitorDelegate
{
    readonly SampleSqliteConnection conn;
    public MyBeaconMonitorDelegate(SampleSqliteConnection conn) => this.conn = conn;
    public Task OnStatusChanged(BeaconRegionState newStatus, BeaconRegion region) => this.conn.Log(    
        $"{region.Identifier} was {newStatus.ToString().ToLower()}",
        $"UUID: {region.Uuid} - M: {region.Major} - m: {region.Minor}"
    );
}
