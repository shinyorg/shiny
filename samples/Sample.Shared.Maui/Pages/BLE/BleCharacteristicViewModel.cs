namespace Sample.Shared.Maui.Pages.BLE;

[ShellMap<BleCharacteristicPage>("blecharacteristic")]
public partial class BleCharacteristicViewModel(IBleManager bleManager) : ObservableObject, IQueryAttributable, IPageLifecycleAware, IDisposable
{
    IPeripheral? peripheral;
    string? serviceUuid;

    [ObservableProperty] string title = "Characteristics";
    [ObservableProperty] string status = string.Empty;

    public List<BleCharacteristicItemViewModel> Characteristics
    {
        get;
        private set
        {
            field = value;
            this.OnPropertyChanged();
        }
    } = [];

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("PeripheralUuid", out var uuid))
            this.peripheral = bleManager.GetKnownPeripheral(uuid!.ToString()!);

        if (query.TryGetValue("ServiceUuid", out var svc))
            this.serviceUuid = svc!.ToString();

        this.Title = $"Service {this.serviceUuid}";
    }

    public void OnAppearing()
    {
        if (this.peripheral == null || this.serviceUuid == null)
            return;

        this.Status = "Loading characteristics...";
        this.peripheral
            .GetCharacteristics(this.serviceUuid)
            .Subscribe(
                chars => MainThread.BeginInvokeOnMainThread(() =>
                {
                    this.Characteristics = chars.Select(ch => new BleCharacteristicItemViewModel(this.peripheral, ch)).ToList();
                    this.Status = $"{chars.Count} characteristic(s)";
                }),
                ex => MainThread.BeginInvokeOnMainThread(() => this.Status = $"Error: {ex.Message}")
            );
    }

    public void OnDisappearing() { }

    public void Dispose()
    {
        foreach (var ch in this.Characteristics)
            ch.Dispose();
    }
}

public partial class BleCharacteristicItemViewModel : ObservableObject, IDisposable
{
    readonly IPeripheral peripheral;
    readonly BleCharacteristicInfo info;
    IDisposable? notifySub;

    public BleCharacteristicItemViewModel(IPeripheral peripheral, BleCharacteristicInfo info)
    {
        this.peripheral = peripheral;
        this.info = info;
        this.Uuid = info.Uuid;
        this.Properties = info.Properties.ToString();
        this.CanRead = info.CanRead();
        this.CanWrite = info.CanWrite();
        this.CanNotify = info.CanNotifyOrIndicate();
    }

    [ObservableProperty] string uuid;
    [ObservableProperty] string properties;
    [ObservableProperty] string value = string.Empty;
    [ObservableProperty] string writeValue = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(NotifyText))]
    bool isNotifying;

    public bool CanRead { get; }
    public bool CanWrite { get; }
    public bool CanNotify { get; }
    public string NotifyText => this.IsNotifying ? "Stop Notify" : "Notify";

    [RelayCommand]
    void Read()
    {
        this.peripheral
            .ReadCharacteristic(this.info.Service.Uuid, this.info.Uuid)
            .Subscribe(
                result => MainThread.BeginInvokeOnMainThread(() =>
                    this.Value = result.Data != null ? $"[{BitConverter.ToString(result.Data)}]" : "[empty]"
                ),
                ex => MainThread.BeginInvokeOnMainThread(() => this.Value = $"Read error: {ex.Message}")
            );
    }

    [RelayCommand]
    void ToggleNotify()
    {
        if (this.IsNotifying)
        {
            this.notifySub?.Dispose();
            this.notifySub = null;
            this.IsNotifying = false;
            return;
        }

        this.IsNotifying = true;
        this.notifySub = this.peripheral
            .NotifyCharacteristic(this.info.Service.Uuid, this.info.Uuid)
            .Subscribe(
                result => MainThread.BeginInvokeOnMainThread(() =>
                    this.Value = result.Data != null ? $"[{BitConverter.ToString(result.Data)}]" : "[empty]"
                ),
                ex => MainThread.BeginInvokeOnMainThread(() =>
                {
                    this.Value = $"Notify error: {ex.Message}";
                    this.IsNotifying = false;
                })
            );
    }

    [RelayCommand]
    void Write()
    {
        if (string.IsNullOrWhiteSpace(this.WriteValue))
            return;

        byte[] data;
        try
        {
            var hex = this.WriteValue.Replace("-", "").Replace(" ", "");
            data = new byte[hex.Length / 2];
            for (var i = 0; i < data.Length; i++)
                data[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
        }
        catch
        {
            this.Value = "Invalid hex input";
            return;
        }

        this.peripheral
            .WriteCharacteristic(this.info.Service.Uuid, this.info.Uuid, data)
            .Subscribe(
                result => MainThread.BeginInvokeOnMainThread(() =>
                    this.Value = result.Data != null ? $"Wrote [{BitConverter.ToString(result.Data)}]" : "Write OK"
                ),
                ex => MainThread.BeginInvokeOnMainThread(() => this.Value = $"Write error: {ex.Message}")
            );
    }

    public void Dispose() => this.notifySub?.Dispose();
}
