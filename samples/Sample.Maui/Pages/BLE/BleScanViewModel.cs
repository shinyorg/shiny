namespace Sample.Maui.Pages.BLE;

[ShellMap<BleScanPage>("blescan")]
public partial class BleScanViewModel(IBleManager bleManager) : ObservableObject, IDisposable
{
    IDisposable? _scanSub;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ScanText))]
    bool isScanning;

    public string ScanText => IsScanning ? "Stop Scan" : "Start Scan";

    public ObservableCollection<PeripheralViewModel> Peripherals { get; } = [];

    [RelayCommand]
    async Task ToggleScan()
    {
        if (IsScanning)
        {
            _scanSub?.Dispose();
            _scanSub = null;
            IsScanning = false;
            return;
        }

        var access = await bleManager.RequestAccess();
        if (access != AccessState.Available)
        {
            await App.Current!.Windows[0].Page!.DisplayAlert("Error", $"BLE access is {access}", "OK");
            return;
        }

        Peripherals.Clear();
        IsScanning = true;
        _scanSub = bleManager
            .Scan()
            .Subscribe(
                result =>
                {
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        var existing = Peripherals.FirstOrDefault(x => x.Uuid == result.Peripheral.Uuid);
                        if (existing != null)
                        {
                            existing.Rssi = result.Rssi;
                        }
                        else
                        {
                            Peripherals.Add(new PeripheralViewModel
                            {
                                Name = result.Peripheral.Name ?? "Unknown",
                                Uuid = result.Peripheral.Uuid,
                                Rssi = result.Rssi
                            });
                        }
                    });
                },
                ex =>
                {
                    IsScanning = false;
                }
            );
    }

    public void Dispose()
    {
        _scanSub?.Dispose();
    }
}

public partial class PeripheralViewModel : ObservableObject
{
    [ObservableProperty]
    string name = string.Empty;

    [ObservableProperty]
    string uuid = string.Empty;

    [ObservableProperty]
    int rssi;
}
