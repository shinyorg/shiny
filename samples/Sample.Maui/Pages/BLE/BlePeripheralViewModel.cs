namespace Sample.Maui.Pages.BLE;

[ShellMap<BlePeripheralPage>("bleperipheral")]
public partial class BlePeripheralViewModel(IBleManager bleManager, INavigator navigator) : ObservableObject, IQueryAttributable, IPageLifecycleAware, IDisposable
{
    IPeripheral? peripheral;
    IDisposable? statusSub;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ConnectText))]
    ConnectionState connectionState;

    [ObservableProperty] string title = "Peripheral";
    [ObservableProperty] string status = string.Empty;
    public string ConnectText => this.ConnectionState == ConnectionState.Connected ? "Disconnect" : "Connect";

    public List<BleServiceViewModel> Services
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
        {
            this.peripheral = bleManager.GetKnownPeripheral(uuid!.ToString()!);
            if (this.peripheral != null)
                this.Title = this.peripheral.Name ?? this.peripheral.Uuid;
        }
    }

    public void OnAppearing()
    {
        if (this.peripheral == null)
            return;

        this.ConnectionState = this.peripheral.Status;
        this.statusSub = this.peripheral
            .WhenStatusChanged()
            .Subscribe(state => MainThread.BeginInvokeOnMainThread(() =>
            {
                this.ConnectionState = state;
                this.Status = state.ToString();
                if (state == ConnectionState.Connected)
                    this.LoadServices();
                else if (state == ConnectionState.Disconnected)
                    this.Services = [];
            }));

        if (this.peripheral.Status == ConnectionState.Connected)
            this.LoadServices();
    }

    public void OnDisappearing()
    {
        this.statusSub?.Dispose();
        this.statusSub = null;
    }

    void LoadServices()
    {
        if (this.peripheral == null)
            return;

        this.peripheral
            .GetServices()
            .Subscribe(
                services => MainThread.BeginInvokeOnMainThread(() =>
                {
                    this.Services = services.Select(svc => new BleServiceViewModel { Uuid = svc.Uuid }).ToList();
                }),
                ex => MainThread.BeginInvokeOnMainThread(() => this.Status = $"Error: {ex.Message}")
            );
    }

    [RelayCommand]
    void ToggleConnection()
    {
        if (this.peripheral == null)
            return;

        if (this.peripheral.Status == ConnectionState.Connected)
        {
            this.peripheral.CancelConnection();
        }
        else
        {
            this.peripheral.Connect(null);
            this.Status = "Connecting...";
        }
    }

    [RelayCommand]
    async Task SelectService(BleServiceViewModel? svc)
    {
        if (svc == null || this.peripheral == null)
            return;

        await navigator.NavigateTo(
            "blecharacteristic",
            ("PeripheralUuid", this.peripheral.Uuid),
            ("ServiceUuid", svc.Uuid)
        );
    }

    public void Dispose()
    {
        this.statusSub?.Dispose();
        this.peripheral?.CancelConnection();
    }
}

public partial class BleServiceViewModel : ObservableObject
{
    [ObservableProperty] string uuid = string.Empty;
}
