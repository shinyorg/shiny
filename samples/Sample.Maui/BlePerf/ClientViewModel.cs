using System.Reactive.Subjects;
using Shiny.BluetoothLE;

namespace Sample.BlePerf;


public class ClientViewModel : ViewModel
{
    readonly IBleManager bleManager;
    readonly Subject<byte[]> packetSubj = new();
    CancellationTokenSource cts = null!;
    IPeripheral peripheral = null!;


    public ClientViewModel(BaseServices services, IBleManager bleManager) : base(services)
    {
        this.bleManager = bleManager;

        this.ToggleGatt = this.TaskCommand("GATT", async (p, ct) =>
        {
            var ch = await p
                .GetKnownCharacteristicAsync(Constants.ServiceUuid, Constants.CharacteristicUuid)
                .ConfigureAwait(false);

            if (ch == null)
                throw new InvalidOperationException("Characteristic Not Found");

            this.Mtu = await p.TryRequestMtu(512).ToTask().ConfigureAwait(false);
            var rnd = new Random();
            var buffer = new byte[this.Mtu];

            while (!ct.IsCancellationRequested)
            {
                rnd.NextBytes(buffer);
                await ch.WriteAsync(buffer, this.WriteWithResponse, ct).ConfigureAwait(false);
                this.packetSubj.OnNext(buffer);
            }
        });

        this.ToggleL2Cap = this.TaskCommand("L2CAP", async (p, ct) =>
        {
            var channel = await p.TryOpenL2CapChannel((ushort)this.Psm, this.Secure);
            if (channel == null)
                throw new InvalidOperationException("Could not create L2Cap");

            var rnd = new Random();
            var buffer = new byte[512];

            while (!ct.IsCancellationRequested)
            {
                rnd.NextBytes(buffer);
                await channel.Write(buffer);
                this.packetSubj.OnNext(buffer);
            }
        });
    }


    [Reactive] public string Status { get; private set; }
    [Reactive] public bool IsConnected { get; private set; }

    [Reactive] public string TransferSpeed { get; private set; }
    [Reactive] public long BytesSent { get; private set; }
    [Reactive] public long PacketCount { get; private set; }

    [Reactive] public bool WriteWithResponse { get; set; } = true;
    [Reactive] public bool Secure { get; set; }
    [Reactive] public int Psm { get; set; } = 92;
    [Reactive] public int Mtu { get; private set; }
    
    public ICommand ToggleGatt { get; }
    public ICommand ToggleL2Cap { get; }
    public override void OnNavigatedFrom(INavigationParameters parameters) => this.Stop();


    ICommand TaskCommand(string type, Func<IPeripheral, CancellationToken, Task> looper) => ReactiveCommand.CreateFromTask(async () =>
    {
        if (this.IsConnected)
        {
            this.Stop();
        }
        else
        { 
            this.Status = "Searching for BLE Perf Testing Device";
            this.peripheral = await this.FindDevice();
            this.Status = "Device Connected";
            this.IsConnected = true;

            this.cts = new();

            // writer loop
            _ = Task.Run(async () =>
            {
                try
                {
                    await looper.Invoke(this.peripheral, this.cts.Token);
                }
                catch (Exception ex)
                {
                    this.Logger.LogError(ex, "Failed to run write loop");
                    this.Stop();
                }
            });
        }
    });


    void Stop()
    {
        this.IsConnected = false;
        this.peripheral?.CancelConnection();
        this.peripheral = null!;        
    }


    Task<IPeripheral> FindDevice() => this.bleManager
        .Scan(new ScanConfig(
            ServiceUuids: Constants.ServiceUuid
        ))
        .Take(1)
        .Timeout(TimeSpan.FromSeconds(30))
        .Select(x => x.Peripheral)
        .Select(x => x.WithConnectIf())
        .Switch()
        .ToTask();
}