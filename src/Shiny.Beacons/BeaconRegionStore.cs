using System;


namespace Shiny.Beacons
{
    public class BeaconRegionStore
    {
        public string Identifier { get; set; }
        public Guid Uuid { get; set; }
        public ushort? Major { get; set; }
        public ushort? Minor { get; set; }
    }
}
