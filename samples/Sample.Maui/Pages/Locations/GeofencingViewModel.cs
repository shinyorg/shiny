namespace Sample.Maui.Pages.Locations;

[ShellMap<GeofencingPage>("geofencing")]
public partial class GeofencingViewModel(IGeofenceManager geofenceManager, IGpsManager gpsManager) : ObservableObject, IPageLifecycleAware
{
    [ObservableProperty]
    string status = string.Empty;

    public ObservableCollection<GeofenceRegionViewModel> Regions { get; } = [];

    public void OnAppearing() => LoadRegions();

    public void OnDisappearing() { }

    void LoadRegions()
    {
        Regions.Clear();
        foreach (var region in geofenceManager.GetMonitorRegions())
        {
            Regions.Add(new GeofenceRegionViewModel
            {
                Identifier = region.Identifier,
                Position = $"{region.Center.Latitude:F4}, {region.Center.Longitude:F4}",
                CurrentState = "Monitoring"
            });
        }
    }

    [RelayCommand]
    async Task AddGeofence()
    {
        var access = await geofenceManager.RequestAccess();
        if (access != AccessState.Available)
        {
            Status = $"Access: {access}";
            return;
        }

        var gpsAccess = await gpsManager.RequestAccess(new GpsRequest());
        if (gpsAccess != AccessState.Available)
        {
            Status = $"GPS Access: {gpsAccess}";
            return;
        }

        var reading = await gpsManager.GetLastReading();
        if (reading == null)
        {
            Status = "No GPS reading available";
            return;
        }

        var region = new GeofenceRegion(
            $"Region_{Regions.Count + 1}",
            new Position(reading.Position.Latitude, reading.Position.Longitude),
            Distance.FromMeters(200)
        );
        await geofenceManager.StartMonitoring(region);
        Status = $"Added {region.Identifier}";
        LoadRegions();
    }

    [RelayCommand]
    async Task RemoveAll()
    {
        await geofenceManager.StopAllMonitoring();
        Regions.Clear();
        Status = "All regions removed";
    }
}

public partial class GeofenceRegionViewModel : ObservableObject
{
    [ObservableProperty] string identifier = string.Empty;
    [ObservableProperty] string position = string.Empty;
    [ObservableProperty] string currentState = string.Empty;
}
