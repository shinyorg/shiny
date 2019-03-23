using System;
using Shiny.Locations;


namespace Shiny.Tests
{
    public class TestAllDelegate : IGeofenceDelegate
    {
        public void OnStatusChanged(GeofenceState newStatus, GeofenceRegion region)
        {
        }
    }
}
