using Shiny.Beacons;

namespace Sample.Shared.Maui.Pages.Beacons;


[ShellMap<BeaconMonitoringPage>("beaconmonitoring")]
public partial class BeaconMonitoringViewModel(
    IBeaconMonitoringManager monitoringManager,
    IDialogs dialogs
) : ObservableObject, IPageLifecycleAware
{
    [ObservableProperty] string status = string.Empty;
    [ObservableProperty] string accessStatus = string.Empty;
    [ObservableProperty] string identifier = string.Empty;
    [ObservableProperty] string uuid = "B9407F30-F5F8-466E-AFF9-25556B57FE6D";
    [ObservableProperty] string major = string.Empty;
    [ObservableProperty] string minor = string.Empty;
    [ObservableProperty] bool notifyOnEntry = true;
    [ObservableProperty] bool notifyOnExit = true;

    public List<MonitoredBeaconRegionViewModel> Regions
    {
        get;
        private set
        {
            field = value;
            this.OnPropertyChanged();
        }
    } = [];


    public void OnAppearing()
    {
        this.AccessStatus = monitoringManager.CurrentStatus.ToString();
        this.LoadRegions();
    }

    public void OnDisappearing() { }


    [RelayCommand]
    async Task AddRegion()
    {
        if (!Guid.TryParse(this.Uuid, out var uuid))
        {
            this.Status = "Enter a valid proximity UUID";
            return;
        }

        var access = await monitoringManager.RequestAccess();
        this.AccessStatus = access.ToString();

        if (access != AccessState.Available)
        {
            this.Status = $"Monitoring needs access - got {access}";
            return;
        }

        ushort? major = UInt16.TryParse(this.Major, out var m) ? m : null;
        ushort? minor = major != null && UInt16.TryParse(this.Minor, out var n) ? n : null;

        var id = this.Identifier.IsEmpty()
            ? $"Beacons_{this.Regions.Count + 1}"
            : this.Identifier.Trim();

        try
        {
            var region = new BeaconRegion(id, uuid, major, minor, this.NotifyOnEntry, this.NotifyOnExit);
            await monitoringManager.StartMonitoring(region);

            this.Status = $"Monitoring {id}";
            this.Identifier = string.Empty;
            this.LoadRegions();
        }
        catch (Exception ex)
        {
            this.Status = ex.Message;
        }
    }


    [RelayCommand]
    async Task RemoveRegion(string identifier)
    {
        if (identifier.IsEmpty())
            return;

        if (!await dialogs.Confirm("Stop Monitoring", $"Stop monitoring '{identifier}'?"))
            return;

        await monitoringManager.StopMonitoring(identifier);
        this.Status = $"Removed {identifier}";
        this.LoadRegions();
    }


    [RelayCommand]
    async Task StopAll()
    {
        if (this.Regions.Count == 0)
            return;

        if (!await dialogs.Confirm("Stop All", $"Stop monitoring all {this.Regions.Count} region(s)?"))
            return;

        await monitoringManager.StopAllMonitoring();
        this.Regions = [];
        this.Status = "All regions removed";
    }


    void LoadRegions() => this.Regions = monitoringManager
        .GetMonitoredRegions()
        .Select(region => new MonitoredBeaconRegionViewModel
        {
            Identifier = region.Identifier,
            Description = region.Major == null
                ? $"{region.Uuid} (any major/minor)"
                : $"{region.Uuid} · {region.Major}/{region.Minor?.ToString() ?? "any"}",
            CurrentState = "Monitoring"
        })
        .ToList();
}


public partial class MonitoredBeaconRegionViewModel : ObservableObject
{
    [ObservableProperty] string identifier = string.Empty;
    [ObservableProperty] string description = string.Empty;
    [ObservableProperty] string currentState = string.Empty;
}
