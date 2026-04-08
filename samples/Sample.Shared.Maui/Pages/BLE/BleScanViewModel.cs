using Shiny.Infrastructure;

namespace Sample.Shared.Maui.Pages.BLE;

[ShellMap<BleScanPage>("blescan")]
public partial class BleScanViewModel(
    IBleManager bleManager,
    INavigator navigator,
    IMainThread mainThread,
    IDialogs dialogs
) : ObservableObject, IDisposable
{
    IDisposable? scanSub;
    readonly Dictionary<string, PeripheralViewModel> lookup = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ScanText))]
    bool isScanning;

    [ObservableProperty] string status = "Idle";
    [ObservableProperty] string serviceUuidFilter = string.Empty;
    [ObservableProperty] string nameFilter = string.Empty;
    [ObservableProperty] int rssiThreshold = -100;
    [ObservableProperty] int discoveredCount;
    [ObservableProperty] int visibleCount;

    public string ScanText => this.IsScanning ? "Stop Scan" : "Start Scan";

    public ObservableCollection<PeripheralViewModel> Peripherals { get; } = [];

    partial void OnNameFilterChanged(string value) => this.RefreshVisibility();
    partial void OnRssiThresholdChanged(int value) => this.RefreshVisibility();

    [RelayCommand]
    async Task ToggleScan()
    {
        if (this.IsScanning)
        {
            this.StopScan();
            return;
        }

        var access = await bleManager.RequestAccess();
        if (access != AccessState.Available)
        {
            await dialogs.Alert("Error", $"BLE access is {access}", "OK");
            return;
        }

        this.lookup.Clear();
        this.Peripherals.Clear();
        this.DiscoveredCount = 0;
        this.VisibleCount = 0;

        var config = this.BuildScanConfig();
        this.IsScanning = true;
        this.Status = config == null
            ? "Scanning (all)"
            : $"Scanning (filter: {string.Join(", ", config.ServiceUuids)})";

        // Buffer scan hits and coalesce per-peripheral so the UI thread isn't flooded —
        // macOS (and other platforms) can emit many advertisements per second per device.
        this.scanSub = bleManager
            .Scan(config)
            .Buffer(TimeSpan.FromMilliseconds(250))
            .Where(batch => batch.Count > 0)
            .Subscribe(
                // TODO: this need to be actually pushed on the macOS thread
                batch => mainThread.BeginInvokeOnMainThread(() =>
                {
                    var latest = new Dictionary<string, ScanResult>(batch.Count);
                    foreach (var r in batch)
                        latest[r.Peripheral.Uuid] = r;
                    foreach (var r in latest.Values)
                        this.OnScanResult(r);
                }),
                ex => mainThread.BeginInvokeOnMainThread(() =>
                {
                    this.Status = $"Scan error: {ex.Message}";
                    this.IsScanning = false;
                })
            );
    }

    [RelayCommand]
    void ClearResults()
    {
        this.lookup.Clear();
        this.Peripherals.Clear();
        this.DiscoveredCount = 0;
        this.VisibleCount = 0;
    }

    [RelayCommand]
    async Task SelectPeripheral(PeripheralViewModel? peripheral)
    {
        if (peripheral == null)
            return;

        this.StopScan();
        await navigator.NavigateTo("bleperipheral", ("PeripheralUuid", peripheral.Uuid));
    }

    ScanConfig? BuildScanConfig()
    {
        if (string.IsNullOrWhiteSpace(this.ServiceUuidFilter))
            return null;

        var uuids = this.ServiceUuidFilter
            .Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToArray();
        return uuids.Length == 0 ? null : new ScanConfig(uuids);
    }

    void OnScanResult(ScanResult result)
    {
        var uuid = result.Peripheral.Uuid;
        var adv = result.AdvertisementData;
        var displayName = result.Peripheral.Name ?? adv.LocalName ?? "Unknown";

        if (this.lookup.TryGetValue(uuid, out var existing))
        {
            existing.Name = displayName;
            existing.Rssi = result.Rssi;
            existing.LastSeen = DateTime.Now;
            existing.SeenCount++;
            existing.IsConnectable = adv.IsConnectable;
            existing.TxPower = adv.TxPower;
            existing.ServiceUuids = FormatServiceUuids(adv.ServiceUuids);
            existing.ManufacturerInfo = FormatManufacturer(adv.ManufacturerData);
            existing.IsVisible = this.MatchesFilter(existing);
        }
        else
        {
            var vm = new PeripheralViewModel
            {
                Uuid = uuid,
                Name = displayName,
                Rssi = result.Rssi,
                LastSeen = DateTime.Now,
                SeenCount = 1,
                IsConnectable = adv.IsConnectable,
                TxPower = adv.TxPower,
                ServiceUuids = FormatServiceUuids(adv.ServiceUuids),
                ManufacturerInfo = FormatManufacturer(adv.ManufacturerData)
            };
            vm.IsVisible = this.MatchesFilter(vm);
            this.lookup[uuid] = vm;
            this.Peripherals.Add(vm);
            this.DiscoveredCount = this.lookup.Count;
        }

        this.RecountVisible();
    }

    bool MatchesFilter(PeripheralViewModel vm)
    {
        if (vm.Rssi < this.RssiThreshold)
            return false;
        if (!string.IsNullOrWhiteSpace(this.NameFilter) &&
            vm.Name.IndexOf(this.NameFilter, StringComparison.OrdinalIgnoreCase) < 0)
            return false;
        return true;
    }

    void RefreshVisibility()
    {
        foreach (var vm in this.lookup.Values)
            vm.IsVisible = this.MatchesFilter(vm);
        this.RecountVisible();
    }

    void RecountVisible() => this.VisibleCount = this.lookup.Values.Count(x => x.IsVisible);

    static string FormatServiceUuids(string[]? uuids)
        => uuids == null || uuids.Length == 0 ? "—" : string.Join(", ", uuids);

    static string FormatManufacturer(ManufacturerData? data)
        => data == null ? "—" : $"0x{data.CompanyId:X4} ({data.Data.Length}b)";

    void StopScan()
    {
        this.scanSub?.Dispose();
        this.scanSub = null;
        this.IsScanning = false;
        this.Status = $"Stopped ({this.lookup.Count} found)";
    }

    public void Dispose() => this.scanSub?.Dispose();
}

public partial class PeripheralViewModel : ObservableObject
{
    [ObservableProperty] string name = string.Empty;
    [ObservableProperty] string uuid = string.Empty;
    [ObservableProperty] int rssi;
    [ObservableProperty] int? txPower;
    [ObservableProperty] bool? isConnectable;
    [ObservableProperty] string serviceUuids = "—";
    [ObservableProperty] string manufacturerInfo = "—";
    [ObservableProperty] DateTime lastSeen;
    [ObservableProperty] int seenCount;
    [ObservableProperty] bool isVisible = true;
}
