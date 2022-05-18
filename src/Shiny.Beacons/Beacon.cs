using System;

namespace Shiny.Beacons;


public record Beacon(
    Guid Uuid,
    ushort Major,
    ushort Minor,    
    Proximity Proximity,
    int Rssi,
    double Accuracy
)
{
    public override string ToString() => $"[Beacon: Uuid={this.Uuid}, Major={this.Major}, Minor={this.Minor}]";
}