using System.Reactive.Threading.Tasks;
using System.Text;
using System.Threading;
using Shiny.BluetoothLE.Hosting;

namespace Sample.Shared.Maui.Pages.BLE;


[ShellMap<BleL2CapPage>("blel2cap")]
public partial class BleL2CapViewModel(IBleManager bleManager, IBleHostingManager hostingManager) : ObservableObject, IDisposable
{
    L2CapInstance? hostInstance;
    L2CapChannel? hostChannel;
    L2CapChannel? clientChannel;
    IDisposable? clientOpenSub;
    IDisposable? clientRxSub;
    IDisposable? hostRxSub;
    Shiny.BluetoothLE.IPeripheral? clientPeripheral;
    int hostConnections;
    int hostBytes;
    int clientBytesSent;
    int clientBytesReceived;

    [ObservableProperty] string status = "Idle";

    // Host
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HostButtonText))]
    bool isHosting;
    [ObservableProperty] bool hostSecure;
    [ObservableProperty] ushort hostPsm;
    [ObservableProperty] int hostConnectionsDisplay;
    [ObservableProperty] int hostBytesReceived;
    [ObservableProperty] string hostLastPeer = "(none)";
    public string HostButtonText => this.IsHosting ? "Stop Host" : "Start Host";

    // Client
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ClientButtonText))]
    [NotifyPropertyChangedFor(nameof(IsConnected))]
    bool isConnecting;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ClientButtonText))]
    [NotifyPropertyChangedFor(nameof(IsConnected))]
    bool isConnectedInternal;
    [ObservableProperty] string peerUuid = string.Empty;
    [ObservableProperty] string peerPsm = string.Empty;
    [ObservableProperty] bool clientSecure;
    [ObservableProperty] string sendPayload = "ping";
    [ObservableProperty] int clientBytesSentDisplay;
    [ObservableProperty] int clientBytesReceivedDisplay;
    [ObservableProperty] string lastReceived = "(none)";
    public bool IsConnected => this.IsConnectedInternal;
    public string ClientButtonText => this.IsConnectedInternal
        ? "Disconnect"
        : this.IsConnecting ? "Cancel" : "Connect";


    [RelayCommand]
    async Task ToggleHost()
    {
        if (this.IsHosting)
        {
            this.hostChannel?.Dispose();
            this.hostChannel = null;
            this.hostInstance?.Dispose();
            this.hostInstance = null;
            this.hostRxSub?.Dispose();
            this.hostRxSub = null;
            this.IsHosting = false;
            this.HostPsm = 0;
            this.Status = "Host stopped";
            return;
        }

        try
        {
            var access = await hostingManager.RequestAccess(advertise: false, connect: true);
            if (access != AccessState.Available)
            {
                this.Status = $"Access denied: {access}";
                return;
            }

            this.hostInstance = await hostingManager.OpenL2Cap(this.HostSecure, channel =>
            {
                Interlocked.Increment(ref this.hostConnections);
                this.hostChannel = channel;

                this.hostRxSub?.Dispose();
                this.hostRxSub = channel.DataReceived.Subscribe(
                    data =>
                    {
                        Interlocked.Add(ref this.hostBytes, data.Length);
                        var preview = Encoding.UTF8.GetString(data);
                        MainThread.BeginInvokeOnMainThread(() =>
                        {
                            this.HostBytesReceived = this.hostBytes;
                            this.HostLastPeer = $"{channel.Identifier}: {preview}";
                            this.Status = $"Host RX {data.Length} bytes";
                        });
                    },
                    _ => { },
                    () => MainThread.BeginInvokeOnMainThread(() =>
                    {
                        Interlocked.Decrement(ref this.hostConnections);
                        this.HostConnectionsDisplay = this.hostConnections;
                    })
                );

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    this.HostConnectionsDisplay = this.hostConnections;
                    this.Status = $"Central connected: {channel.Identifier}";
                });
            });

            this.HostPsm = this.hostInstance.Value.Psm;
            this.IsHosting = true;
            this.Status = $"Hosting PSM {this.HostPsm}";
        }
        catch (Exception ex)
        {
            this.Status = $"Host error: {ex.Message}";
        }
    }


    [RelayCommand]
    void ToggleClient()
    {
        if (this.IsConnectedInternal || this.IsConnecting)
        {
            this.clientOpenSub?.Dispose();
            this.clientRxSub?.Dispose();
            this.clientChannel?.Dispose();
            this.clientChannel = null;
            this.clientPeripheral?.CancelConnection();
            this.clientPeripheral = null;
            this.IsConnectedInternal = false;
            this.IsConnecting = false;
            this.Status = "Client disconnected";
            return;
        }

        if (string.IsNullOrWhiteSpace(this.PeerUuid) || !ushort.TryParse(this.PeerPsm, out var psm))
        {
            this.Status = "Enter peer UUID/MAC and a numeric PSM";
            return;
        }

        var peripheral = bleManager.GetKnownPeripheral(this.PeerUuid);
        if (peripheral == null)
        {
            this.Status = "Peripheral unknown. Scan it first from the Scanner page.";
            return;
        }
        this.clientPeripheral = peripheral;

        if (peripheral is not Shiny.BluetoothLE.ICanL2Cap)
        {
            this.Status = "This platform does not support L2CAP.";
            return;
        }

        this.IsConnecting = true;
        this.Status = "Connecting GATT...";

        // Connect GATT first if needed — Apple opens L2CAP via CBPeripheral which requires the
        // GATT connection. Android allows L2CAP without GATT, but connecting is harmless. Linux
        // routes through the kernel socket layer directly so this is also fine.
        if (peripheral.Status != ConnectionState.Connected)
            peripheral.Connect(null);

        var subj = peripheral
            .WhenStatusChanged()
            .Where(s => s == ConnectionState.Connected)
            .Take(1)
            .SelectMany(_ => ((Shiny.BluetoothLE.ICanL2Cap)peripheral).OpenL2CapChannel(psm, this.ClientSecure));

        this.clientOpenSub = subj.Subscribe(
            channel =>
            {
                this.clientChannel = channel;
                this.clientRxSub = channel.DataReceived.Subscribe(data =>
                {
                    Interlocked.Add(ref this.clientBytesReceived, data.Length);
                    var preview = Encoding.UTF8.GetString(data);
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        this.ClientBytesReceivedDisplay = this.clientBytesReceived;
                        this.LastReceived = preview;
                    });
                });
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    this.IsConnecting = false;
                    this.IsConnectedInternal = true;
                    this.Status = $"L2CAP open on PSM {channel.Psm}";
                });
            },
            ex => MainThread.BeginInvokeOnMainThread(() =>
            {
                this.IsConnecting = false;
                this.Status = $"Client error: {ex.Message}";
            })
        );
    }


    [RelayCommand]
    async Task Send()
    {
        if (this.clientChannel == null)
            return;

        var data = Encoding.UTF8.GetBytes(this.SendPayload);
        try
        {
            await this.clientChannel.Write(data).ToTask();
            Interlocked.Add(ref this.clientBytesSent, data.Length);
            this.ClientBytesSentDisplay = this.clientBytesSent;
            this.Status = $"Sent {data.Length} bytes";
        }
        catch (Exception ex)
        {
            this.Status = $"Send error: {ex.Message}";
        }
    }


    public void Dispose()
    {
        this.hostRxSub?.Dispose();
        this.hostChannel?.Dispose();
        this.hostInstance?.Dispose();
        this.clientOpenSub?.Dispose();
        this.clientRxSub?.Dispose();
        this.clientChannel?.Dispose();
        this.clientPeripheral?.CancelConnection();
    }
}
