namespace Sample.Shared.Maui.Pages.Locations;

[ShellMap<RegionGeofencingPage>("regiongeofencing")]
public partial class RegionGeofencingViewModel(IRegionGeofenceManager regions) : ObservableObject, IPageLifecycleAware
{
    // the set names registered in ShinyRegistrations
    public const string StatesSet = "regions";
    public const string CitiesSet = "cities";
    public const string GeocodedSet = "geocoded-cities";

    [ObservableProperty] string monitoringText = string.Empty;
    [ObservableProperty] string toggleText = string.Empty;
    [ObservableProperty] string status = string.Empty;
    [ObservableProperty] string stateRegion = "-";
    [ObservableProperty] string cityRegion = "-";
    [ObservableProperty] string geocodedRegion = "-";


    public void OnAppearing()
    {
        this.UpdateMonitoring();
        if (regions.IsStarted)
            this.RefreshCommand.Execute(null);
    }

    public void OnDisappearing() { }


    [RelayCommand]
    async Task Toggle()
    {
        try
        {
            if (regions.IsStarted)
            {
                await regions.Stop();
                this.Status = "Stopped - current regions forgotten";
            }
            else
            {
                var access = await regions.RequestAccess();
                if (access == AccessState.Available)
                {
                    await regions.Start();
                    this.Status = "Started - move around to cross a region";
                }
                else
                {
                    this.Status = $"Location access: {access}";
                }
            }
        }
        catch (InvalidOperationException ex)
        {
            // GpsManager allows one listener - the GPS page may already have one running
            this.Status = $"{ex.Message} - stop it on the GPS page first";
        }
        catch (Exception ex)
        {
            this.Status = ex.Message;
        }
        this.UpdateMonitoring();
    }


    [RelayCommand]
    async Task Refresh()
    {
        try
        {
            // one lookup per set against the last GPS reading - no events, no state change
            var current = await regions.GetCurrent();
            if (current.Count == 0)
            {
                this.Status = "No GPS reading yet";
            }
            else
            {
                this.StateRegion = Describe(current, StatesSet);
                this.CityRegion = Describe(current, CitiesSet);
                this.GeocodedRegion = Describe(current, GeocodedSet);
                this.Status = $"Updated {DateTime.Now:T}";
            }
        }
        catch (Exception ex)
        {
            // the geocoder set needs a connection
            this.Status = ex.Message;
        }
    }


    void UpdateMonitoring()
    {
        this.MonitoringText = regions.IsStarted ? "Running" : "Stopped";
        this.ToggleText = regions.IsStarted ? "Stop Monitoring" : "Start Monitoring";
    }


    static string Describe(IReadOnlyList<CurrentRegion> current, string set)
        => current.FirstOrDefault(x => x.RegionSet == set) is { RegionId: not null } region
            ? region.RegionName ?? region.RegionId
            : "Outside";
}
