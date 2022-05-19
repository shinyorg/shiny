using System;


namespace Shiny.Beacons;

public enum BeaconRegisterEventType
{
    Add,
    Update,
    Remove,
    Clear
}


public record BeaconRegisterEvent(
    BeaconRegisterEventType EventType, 
    BeaconRegion? Region
);
