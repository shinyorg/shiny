using System;


namespace Shiny.Locations
{
    public struct GeofenceCurrentStatus
    {
        public GeofenceCurrentStatus(GeofenceRegion region, GeofenceState status)
        {
            this.Region = region;
            this.Status = status;
        }

        public GeofenceRegion Region { get; }
        public GeofenceState Status { get; }
    }
}
