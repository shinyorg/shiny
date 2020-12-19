using System;
using System.Windows.Input;
using Shiny.Locations;


namespace Samples.Geofences
{
    public class GeofenceRegionViewModel
    {
        public GeofenceRegion Region { get; set; }
        public ICommand RequestCurrentState { get; set; }
        public ICommand Remove { get; set; }


        public string Text => $"{this.Region.Identifier}";
        public string Detail => $"{this.Region.Radius.TotalMeters}m from {this.Region.Center.Latitude}/{this.Region.Center.Longitude}";
    }
}
