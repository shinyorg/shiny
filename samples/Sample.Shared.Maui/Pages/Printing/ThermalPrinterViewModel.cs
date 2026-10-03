using Shiny.Infrastructure;
using Shiny.Printers;
using Shiny.Printers.BluetoothLE;
using Shiny.Printers.Network;

namespace Sample.Shared.Maui.Pages.Printing;


[ShellMap<ThermalPrinterPage>("thermalprinter")]
public partial class ThermalPrinterViewModel(
    IPrinterScanner bleScanner,
    BlePrinterManager bleManager,
    INetworkPrinterScanner networkScanner,
    NetworkPrinterManager networkManager,
    IMainThread mainThread,
    IDialogs dialogs
) : ObservableObject, IDisposable
{
    IDisposable? bleScanSub;
    IDisposable? networkScanSub;
    IPrinter? printer;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(BleScanText))]
    bool isBleScanning;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(NetworkScanText))]
    bool isNetworkScanning;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ConnectManualCommand))]
    [NotifyCanExecuteChangedFor(nameof(PrintCommand))]
    [NotifyCanExecuteChangedFor(nameof(DisconnectPrinterCommand))]
    bool isBusy;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsDisconnected))]
    [NotifyCanExecuteChangedFor(nameof(PrintCommand))]
    [NotifyCanExecuteChangedFor(nameof(DisconnectPrinterCommand))]
    bool isConnected;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ConnectManualCommand))]
    string manualHost = string.Empty;

    [ObservableProperty] string manualPort = "9100";
    [ObservableProperty] bool manual80mm = true;
    [ObservableProperty] string status = "Scan for a Bluetooth or WiFi printer";
    [ObservableProperty] string capabilitiesText = string.Empty;

    public bool IsDisconnected => !this.IsConnected;
    public string BleScanText => this.IsBleScanning ? "Stop BLE Scan" : "Scan BLE";
    public string NetworkScanText => this.IsNetworkScanning ? "Stop WiFi Scan" : "Scan WiFi";

    // plain lists reassigned in bulk - they are small and always replaced wholesale
    public List<DiscoveredPrinter> BlePrinters
    {
        get;
        private set
        {
            field = value;
            this.OnPropertyChanged();
        }
    } = [];

    public List<DiscoveredNetworkPrinter> NetworkPrinters
    {
        get;
        private set
        {
            field = value;
            this.OnPropertyChanged();
        }
    } = [];


    [RelayCommand]
    void ToggleBleScan()
    {
        if (this.IsBleScanning)
        {
            this.StopBleScan();
            return;
        }

        this.BlePrinters = [];
        this.IsBleScanning = true;
        this.Status = "Scanning for BLE printers...";

        this.bleScanSub = bleScanner
            .Scan()
            .Buffer(TimeSpan.FromMilliseconds(500))
            .Where(batch => batch.Count > 0)
            .Subscribe(
                batch => mainThread.BeginInvokeOnMainThread(() =>
                {
                    // the same printer advertises repeatedly - keep the latest per peripheral
                    this.BlePrinters = this.BlePrinters
                        .Concat(batch)
                        .GroupBy(x => x.Uuid)
                        .Select(x => x.Last())
                        .OrderByDescending(x => x.Rssi)
                        .ToList();
                    this.Status = $"Found {this.BlePrinters.Count} BLE printer(s)";
                }),
                ex => mainThread.BeginInvokeOnMainThread(() =>
                {
                    this.IsBleScanning = false;
                    this.Status = $"BLE scan error: {ex.Message}";
                })
            );
    }


    [RelayCommand]
    void ToggleNetworkScan()
    {
        if (this.IsNetworkScanning)
        {
            this.StopNetworkScan();
            return;
        }

        this.NetworkPrinters = [];
        this.IsNetworkScanning = true;
        this.Status = "Scanning for WiFi printers (mDNS)...";

        // the scanner already de-duplicates by host:port
        this.networkScanSub = networkScanner
            .Scan()
            .Subscribe(
                found => mainThread.BeginInvokeOnMainThread(() =>
                {
                    this.NetworkPrinters = this.NetworkPrinters.Append(found).ToList();
                    this.Status = $"Found {this.NetworkPrinters.Count} WiFi printer(s)";
                }),
                ex => mainThread.BeginInvokeOnMainThread(() =>
                {
                    this.IsNetworkScanning = false;
                    this.Status = $"WiFi scan error: {ex.Message}";
                })
            );
    }


    // CollectionView SelectionChangedCommand - connecting is the only thing selecting a printer does
    [RelayCommand]
    Task ConnectBle(DiscoveredPrinter? selected)
        => selected == null || this.IsBusy
            ? Task.CompletedTask
            : this.ConnectTo(() => bleManager.Connect(selected), selected.Name ?? selected.Uuid);


    [RelayCommand]
    Task ConnectNetwork(DiscoveredNetworkPrinter? selected)
        => selected == null || this.IsBusy
            ? Task.CompletedTask
            : this.ConnectTo(() => networkManager.Connect(selected), $"{selected.Name} ({selected.Host})");


    [RelayCommand(CanExecute = nameof(CanConnectManual))]
    async Task ConnectManual()
    {
        if (!Int32.TryParse(this.ManualPort, out var port) || port is < 1 or > 65535)
        {
            await dialogs.Alert("Invalid Port", "Enter a port between 1 and 65535 (receipt printers use 9100)", "OK");
            return;
        }
        var caps = this.Manual80mm ? PrinterCapabilities.Paper80mm : PrinterCapabilities.Paper58mm;
        await this.ConnectTo(() => networkManager.Connect(this.ManualHost.Trim(), port, caps), $"{this.ManualHost}:{port}");
    }

    bool CanConnectManual() => !String.IsNullOrWhiteSpace(this.ManualHost) && !this.IsBusy;


    async Task ConnectTo(Func<Task<IPrinter>> connect, string target)
    {
        this.StopBleScan();
        this.StopNetworkScan();
        this.Disconnect();

        this.IsBusy = true;
        this.Status = $"Connecting to {target}...";
        try
        {
            var connected = await connect();
            this.printer = connected;

            var caps = connected.Capabilities;
            this.CapabilitiesText = $"{caps.PaperWidthMm}mm  •  {caps.CharactersPerLine} cols  •  {caps.DotsPerLine} dots  •  cut: {(caps.SupportsCut ? "yes" : "no")}";
            this.IsConnected = true;
            this.Status = $"Connected to {target}";
        }
        catch (Exception ex)
        {
            this.Status = "Connect failed";
            await dialogs.Alert("Connect Failed", ex.Message, "OK");
        }
        finally
        {
            this.IsBusy = false;
        }
    }


    [RelayCommand(CanExecute = nameof(CanPrint))]
    async Task Print()
    {
        this.IsBusy = true;
        this.Status = "Printing test receipt...";
        try
        {
            var doc = SampleReceipt.Build(this.printer!.Capabilities);
            await this.printer.Print(doc);
            this.Status = "Receipt sent";
        }
        catch (Exception ex)
        {
            this.Status = "Print failed";
            await dialogs.Alert("Print Failed", ex.Message, "OK");
        }
        finally
        {
            this.IsBusy = false;
        }
    }

    bool CanPrint() => this.IsConnected && !this.IsBusy;


    [RelayCommand(CanExecute = nameof(CanPrint))]
    void DisconnectPrinter()
    {
        this.Disconnect();
        this.Status = "Disconnected";
    }


    void StopBleScan()
    {
        this.bleScanSub?.Dispose();
        this.bleScanSub = null;
        this.IsBleScanning = false;
    }


    void StopNetworkScan()
    {
        this.networkScanSub?.Dispose();
        this.networkScanSub = null;
        this.IsNetworkScanning = false;
    }


    void Disconnect()
    {
        // the managers hand back a Printer, which disconnects its BLE / TCP connection on dispose
        (this.printer as IDisposable)?.Dispose();
        this.printer = null;
        this.IsConnected = false;
        this.CapabilitiesText = String.Empty;
    }


    public void Dispose()
    {
        this.StopBleScan();
        this.StopNetworkScan();
        this.Disconnect();
    }
}
