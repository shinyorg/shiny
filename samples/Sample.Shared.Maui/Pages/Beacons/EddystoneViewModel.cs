using Shiny.Beacons;
using Shiny.Infrastructure;

namespace Sample.Shared.Maui.Pages.Beacons;


[ShellMap<EddystonePage>("eddystone")]
public partial class EddystoneViewModel(
    IEddystoneScanner scanner,
    IMainThread mainThread
) : ObservableObject, IPageLifecycleAware
{
    // one row per peripheral+frame type, so a beacon interleaving UID and TLM shows both, updating
    readonly Dictionary<string, EddystoneFrameViewModel> rows = new();
    IDisposable? scanSub;

    [ObservableProperty] string status = string.Empty;
    [ObservableProperty] string scanText = "Start Scanning";

    public List<EddystoneFrameViewModel> Frames
    {
        get;
        private set
        {
            field = value;
            this.OnPropertyChanged();
        }
    } = [];


    public void OnAppearing() { }
    public void OnDisappearing() => this.StopScan();


    [RelayCommand]
    async Task ToggleScan()
    {
        if (this.scanSub != null)
        {
            this.StopScan();
            return;
        }

        var access = await scanner.RequestAccess();
        if (access != AccessState.Available)
        {
            this.Status = $"Access: {access}";
            return;
        }

        this.scanSub = scanner
            .WhenFrameReceived()
            .Buffer(TimeSpan.FromSeconds(1))
            .Where(x => x.Count > 0)
            .Subscribe(
                frames => mainThread.BeginInvokeOnMainThread(() => this.OnFrames(frames)),
                ex => mainThread.BeginInvokeOnMainThread(() => this.Status = ex.Message)
            );

        this.ScanText = "Stop Scanning";
        this.Status = "Scanning for 0xFEAA service data...";
    }


    [RelayCommand]
    void Clear()
    {
        this.rows.Clear();
        this.Frames = [];
    }


    void StopScan()
    {
        this.scanSub?.Dispose();
        this.scanSub = null;
        this.ScanText = "Start Scanning";
        this.Status = "Stopped";
    }


    void OnFrames(IList<EddystoneFrame> frames)
    {
        foreach (var frame in frames)
        {
            var key = $"{frame.PeripheralId}|{frame.FrameType}";

            if (!this.rows.TryGetValue(key, out var row))
            {
                row = new EddystoneFrameViewModel();
                this.rows[key] = row;
            }

            row.FrameType = frame.FrameType.ToString().ToUpperInvariant();
            row.Rssi = $"{frame.Rssi} dBm";
            row.Detail = $"{frame.PeripheralId} · {frame.Timestamp.LocalDateTime:HH:mm:ss}";
            row.Payload = Describe(frame);
        }

        this.Frames = this.rows.Values.ToList();
    }


    static string Describe(EddystoneFrame frame) => frame switch
    {
        EddystoneUidFrame uid => $"{uid.Uid.Namespace} / {uid.Uid.Instance}  ·  {Distance(uid.Distance)}",
        EddystoneUrlFrame url => $"{url.Url}  ·  {Distance(url.Distance)}",
        EddystoneTlmFrame { IsEncrypted: true } => "encrypted telemetry (identity key required)",
        EddystoneTlmFrame tlm =>
            $"{tlm.BatteryVolts?.ToString("N3") ?? "mains"} V · " +
            $"{tlm.TemperatureCelsius?.ToString("N1") ?? "no sensor"} °C · " +
            $"{tlm.AdvertisementCount:N0} adverts · up {tlm.Uptime:d\\.hh\\:mm\\:ss}",
        EddystoneEidFrame eid => $"EID {eid.EphemeralIdHex}  ·  {Distance(eid.Distance)}",
        _ => string.Empty
    };


    static string Distance(double distance)
        => distance < 0 ? "distance unknown" : $"{distance:N2} m";
}


public partial class EddystoneFrameViewModel : ObservableObject
{
    [ObservableProperty] string frameType = string.Empty;
    [ObservableProperty] string payload = string.Empty;
    [ObservableProperty] string rssi = string.Empty;
    [ObservableProperty] string detail = string.Empty;
}
