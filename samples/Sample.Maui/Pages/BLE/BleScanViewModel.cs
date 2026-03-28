namespace Sample.Maui.Pages.BLE;

[ShellMap<BleScanPage>("blescan")]
public partial class BleScanViewModel(IBleManager bleManager, INavigator navigator) : ObservableObject, IDisposable
{
    IDisposable? scanSub;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ScanText))]
    bool isScanning;

    public string ScanText => this.IsScanning ? "Stop Scan" : "Start Scan";

    public ObservableCollection<PeripheralViewModel> Peripherals { get; } = [];

    [RelayCommand]
    async Task ToggleScan()
    {
        if (this.IsScanning)
        {
            this.scanSub?.Dispose();
            this.scanSub = null;
            this.IsScanning = false;
            return;
        }

        var access = await bleManager.RequestAccess();
        if (access != AccessState.Available)
        {
            await App.Current!.Windows[0].Page!.DisplayAlert("Error", $"BLE access is {access}", "OK");
            return;
        }

        this.Peripherals.Clear();
        this.IsScanning = true;
        this.scanSub = bleManager
            .Scan()
            .Subscribe(
                result =>
                {
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        var existing = this.Peripherals.FirstOrDefault(x => x.Uuid == result.Peripheral.Uuid);
                        if (existing != null)
                        {
                            existing.Rssi = result.Rssi;
                        }
                        else
                        {
                            this.Peripherals.Add(new PeripheralViewModel
                            {
                                Name = result.Peripheral.Name ?? "Unknown",
                                Uuid = result.Peripheral.Uuid,
                                Rssi = result.Rssi
                            });
                        }
                    });
                },
                ex => this.IsScanning = false
            );
    }

    [RelayCommand]
    async Task SelectPeripheral(PeripheralViewModel? peripheral)
    {
        if (peripheral == null)
            return;

        this.scanSub?.Dispose();
        this.scanSub = null;
        this.IsScanning = false;

        await navigator.NavigateTo("bleperipheral", ("PeripheralUuid", peripheral.Uuid));
    }

    public void Dispose() => this.scanSub?.Dispose();
}

public partial class PeripheralViewModel : ObservableObject
{
    [ObservableProperty] string name = string.Empty;
    [ObservableProperty] string uuid = string.Empty;
    [ObservableProperty] int rssi;
}
