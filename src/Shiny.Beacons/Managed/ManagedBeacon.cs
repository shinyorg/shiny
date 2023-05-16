using System;

namespace Shiny.Beacons.Managed;


public class ManagedBeacon : NotifyPropertyChanged
{
    public ManagedBeacon(Beacon beacon, string regionIdentifier)
    {
        this.Beacon = beacon;
        this.RegionIdentifier = regionIdentifier;
    }


    public Beacon Beacon { get; }
    public string RegionIdentifier { get; }


    Proximity prox;
    public Proximity Proximity
    {
        get => this.prox;
        internal set => this.Set(ref this.prox, value);
    }


    DateTimeOffset lastSeen;

    public DateTimeOffset LastSeen
    {
        get => this.lastSeen;
        internal set => this.Set(ref this.lastSeen, value);
    }
}
