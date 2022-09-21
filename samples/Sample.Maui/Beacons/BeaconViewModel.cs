using Shiny.Beacons;

namespace Sample.Beacons;


public class BeaconViewModel : ReactiveObject
{
    public BeaconViewModel(Beacon beacon)
    {
        this.Beacon = beacon;
        this.Proximity = beacon.Proximity;
    }


    public Beacon Beacon { get; }
    public ushort Major => this.Beacon.Major;
    public ushort Minor => this.Beacon.Minor;
    public string RegionIdentifier => $"Major: {this.Major} - Minor: {this.Minor}";

    [Reactive] public Proximity Proximity { get; set; }
}
