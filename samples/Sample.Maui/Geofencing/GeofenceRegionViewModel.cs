using Shiny.Locations;

namespace Sample.Geofencing;


public class GeofenceRegionViewModel
{
    public GeofenceRegionViewModel(GeofenceRegion region, ICommand remove, ICommand requestCurrent)
    {
        this.Region = region;
        this.Remove = remove;
        this.RequestCurrentState = requestCurrent;
    }


    public GeofenceRegion Region { get; }
    public ICommand RequestCurrentState { get; }
    public ICommand Remove { get; }


    public string Text => this.Region.Identifier;
    public string Detail => $"{this.Region.Radius.TotalMeters}m from {this.Region.Center.Latitude}/{this.Region.Center.Longitude}";
}
