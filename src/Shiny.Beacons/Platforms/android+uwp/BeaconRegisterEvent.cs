using System;


namespace Shiny.Beacons
{
    public enum BeaconRegisterEventType
    {
        Add,
        Update,
        Remove,
        Clear
    }


    public class BeaconRegisterEvent
    {
        public BeaconRegisterEvent(BeaconRegisterEventType eventType, BeaconRegion? region = null)
        {
            this.Type = eventType;
            this.Region = region;
        }


        public BeaconRegisterEventType Type { get; }
        public BeaconRegion? Region { get; }
    }
}