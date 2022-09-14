using ReactiveUI.Fody.Helpers;

using Shiny;
using Shiny.Beacons;


namespace Sample
{
    public class BeaconViewModel : NotifyPropertyChanged
    {
        public BeaconViewModel(Beacon beacon)
        {
            this.Beacon = beacon;
            this.Proximity = beacon.Proximity;
        }


        public Beacon Beacon { get; }
        public ushort Major => this.Beacon.Major;
        public ushort Minor => this.Beacon.Minor;
        public string Identifier => $"Major: {this.Major} - Minor: {this.Minor}";

        [Reactive] public Proximity Proximity { get; set; }
    }
}
