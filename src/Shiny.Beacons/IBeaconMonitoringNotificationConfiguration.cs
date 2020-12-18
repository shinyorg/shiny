using System;


namespace Shiny.Beacons
{
    public interface IBeaconMonitoringNotificationConfiguration
    {
        string? Title { get; set; }
        string? Description { get; set; }
        string? Ticker { get; set; }
    }
}
