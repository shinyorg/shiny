using System;


namespace Shiny.Beacons.Managed
{
    public class ManagedBeacon : NotifyPropertyChanged
    {
        public ManagedBeacon(Beacon beacon) => this.Beacon = beacon;
        public Beacon Beacon { get; }


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
}
