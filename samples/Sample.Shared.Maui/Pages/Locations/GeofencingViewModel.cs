namespace Sample.Shared.Maui.Pages.Locations;

[ShellMap<GeofencingPage>("geofencing")]
public partial class GeofencingViewModel(IGeofenceManager geofenceManager, IGpsManager gpsManager, IDialogs dialogs) : ObservableObject, IPageLifecycleAware
{
    [ObservableProperty] string status = string.Empty;
    [ObservableProperty] string identifier = string.Empty;
    [ObservableProperty] string latitude = string.Empty;
    [ObservableProperty] string longitude = string.Empty;
    [ObservableProperty] string radiusMeters = "200";
    [ObservableProperty] bool notifyOnEntry = true;
    [ObservableProperty] bool notifyOnExit = true;
    [ObservableProperty] bool singleUse;

    public List<GeofenceRegionViewModel> Regions
    {
        get;
        private set
        {
            field = value;
            this.OnPropertyChanged();
        }
    } = [];

    public void OnAppearing() => this.LoadRegions();
    public void OnDisappearing() { }

    void LoadRegions()
    {
        this.Regions = geofenceManager.GetMonitorRegions().Select(region => new GeofenceRegionViewModel
        {
            Identifier = region.Identifier,
            Position = $"{region.Center.Latitude:F4}, {region.Center.Longitude:F4}",
            Radius = $"{region.Radius.TotalMeters:F0}m",
            CurrentState = "Monitoring"
        }).ToList();
    }

    [RelayCommand]
    async Task UseCurrentLocation()
    {
        var gpsAccess = await gpsManager.RequestAccess(new GpsRequest());
        if (gpsAccess != AccessState.Available)
        {
            this.Status = $"GPS Access: {gpsAccess}";
            return;
        }

        var reading = await gpsManager.GetLastReading();
        if (reading == null)
        {
            this.Status = "No GPS reading available";
            return;
        }

        this.Latitude = reading.Position.Latitude.ToString("F6");
        this.Longitude = reading.Position.Longitude.ToString("F6");
        this.Status = "Location set from GPS";
    }

    [RelayCommand]
    async Task AddGeofence()
    {
        if (!double.TryParse(this.Latitude, out var lat) || !double.TryParse(this.Longitude, out var lng))
        {
            this.Status = "Enter valid latitude and longitude";
            return;
        }

        if (!double.TryParse(this.RadiusMeters, out var radius) || radius <= 0)
        {
            this.Status = "Enter a valid radius in meters";
            return;
        }

        var access = await geofenceManager.RequestAccess();
        if (access != AccessState.Available)
        {
            this.Status = $"Access: {access}";
            return;
        }

        var id = string.IsNullOrWhiteSpace(this.Identifier)
            ? $"Region_{this.Regions.Count + 1}"
            : this.Identifier.Trim();

        if (this.Regions.Any(r => string.Equals(r.Identifier, id, StringComparison.OrdinalIgnoreCase)))
        {
            this.Status = $"Identifier '{id}' is already in use";
            return;
        }

        var region = new GeofenceRegion(
            id,
            new Position(lat, lng),
            Distance.FromMeters(radius),
            this.SingleUse,
            this.NotifyOnEntry,
            this.NotifyOnExit
        );
        await geofenceManager.StartMonitoring(region);
        this.Status = $"Added {region.Identifier}";
        this.Identifier = string.Empty;
        this.LoadRegions();
    }

    [RelayCommand]
    async Task RemoveGeofence(string identifier)
    {
        if (string.IsNullOrWhiteSpace(identifier))
            return;

        if (!await dialogs.Confirm("Remove Geofence", $"Stop monitoring '{identifier}'?"))
            return;

        await geofenceManager.StopMonitoring(identifier);
        this.Status = $"Removed {identifier}";
        this.LoadRegions();
    }

    [RelayCommand]
    async Task RemoveAll()
    {
        if (this.Regions.Count == 0)
            return;

        if (!await dialogs.Confirm("Remove All Geofences", $"Stop monitoring all {this.Regions.Count} region(s)?"))
            return;

        await geofenceManager.StopAllMonitoring();
        this.Regions = [];
        this.Status = "All regions removed";
    }
}

public partial class GeofenceRegionViewModel : ObservableObject
{
    [ObservableProperty] string identifier = string.Empty;
    [ObservableProperty] string position = string.Empty;
    [ObservableProperty] string radius = string.Empty;
    [ObservableProperty] string currentState = string.Empty;
}
