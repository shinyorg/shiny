using Shiny.Beacons;
using Shiny.Infrastructure;
using Shiny.Beacons.Managed;

namespace Sample.Shared.Maui.Pages.Beacons;


[ShellMap<BeaconRangingPage>("beaconranging")]
public partial class BeaconRangingViewModel : ObservableObject, IPageLifecycleAware, IDisposable
{
    // Estimote's default UUID - the one most test beacons and simulator apps ship with
    const string SampleUuid = "B9407F30-F5F8-466E-AFF9-25556B57FE6D";

    readonly IBeaconRangingManager rangingManager;
    readonly IMainThread mainThread;
    readonly ManagedBeaconScan scan;
    IDisposable? refreshSub;

    public BeaconRangingViewModel(IBeaconRangingManager rangingManager, IMainThread mainThread)
    {
        this.rangingManager = rangingManager;
        this.mainThread = mainThread;
        this.scan = rangingManager.CreateManagedScan();
    }

    [ObservableProperty] string uuid = string.Empty;
    [ObservableProperty] string major = string.Empty;
    [ObservableProperty] string minor = string.Empty;
    [ObservableProperty] string status = string.Empty;
    [ObservableProperty] string scanText = "Start Ranging";

    public List<RangedBeaconViewModel> Beacons
    {
        get;
        private set
        {
            field = value;
            this.OnPropertyChanged();
        }
    } = [];


    public void OnAppearing()
        // ManagedBeaconScan keeps one entry per beacon and updates it in place. Distances move on
        // every advertisement, so the projection is rebuilt on a timer rather than per change -
        // otherwise the list re-sorts faster than it can be read.
        => this.refreshSub = Observable
            .Interval(TimeSpan.FromSeconds(1))
            .Subscribe(_ => this.mainThread.BeginInvokeOnMainThread(this.Refresh));


    public void OnDisappearing()
    {
        this.refreshSub?.Dispose();
        this.refreshSub = null;
        this.StopScan();
    }


    [RelayCommand]
    void UseSampleUuid() => this.Uuid = SampleUuid;


    [RelayCommand]
    async Task ToggleScan()
    {
        if (this.scan.IsScanning)
        {
            this.StopScan();
            return;
        }

        if (!Guid.TryParse(this.Uuid, out var uuid))
        {
            this.Status = "Enter a valid proximity UUID";
            return;
        }

        var access = await this.rangingManager.RequestAccess();
        if (access != AccessState.Available)
        {
            this.Status = $"Access: {access}";
            return;
        }

        ushort? major = UInt16.TryParse(this.Major, out var m) ? m : null;
        ushort? minor = major != null && UInt16.TryParse(this.Minor, out var n) ? n : null;

        try
        {
            var region = new BeaconRegion("sample-ranging", uuid, major, minor);

            // clearTime drops beacons that stop advertising, so the list reflects what is actually
            // in range rather than everything seen since the scan started
            await this.scan.Start(region, clearTime: TimeSpan.FromSeconds(15));

            this.ScanText = "Stop Ranging";
            this.Status = "Ranging...";
        }
        catch (Exception ex)
        {
            this.Status = ex.Message;
        }
    }


    void StopScan()
    {
        this.scan.Stop();
        this.ScanText = "Start Ranging";
        this.Status = "Stopped";
    }


    void Refresh() => this.Beacons = this.scan
        .Beacons
        .OrderBy(x => x.Distance)
        .Select(x => new RangedBeaconViewModel
        {
            Identity = $"{x.Uuid}",
            Proximity = x.Proximity.ToString(),
            Distance = x.Distance < 0 ? "Distance: unknown" : $"Distance: {x.Distance:N2} m  ·  Major {x.Major} / Minor {x.Minor}",
            Rssi = $"{x.Rssi} dBm",
            LastSeen = $"Last seen {x.LastSeen.LocalDateTime:HH:mm:ss}"
        })
        .ToList();


    public void Dispose() => this.scan.Dispose();
}


public partial class RangedBeaconViewModel : ObservableObject
{
    [ObservableProperty] string identity = string.Empty;
    [ObservableProperty] string proximity = string.Empty;
    [ObservableProperty] string distance = string.Empty;
    [ObservableProperty] string rssi = string.Empty;
    [ObservableProperty] string lastSeen = string.Empty;
}
