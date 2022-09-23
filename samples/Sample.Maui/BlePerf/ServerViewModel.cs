using System.Reactive.Disposables;
using System.Reactive.Subjects;
using Humanizer;
using Shiny.BluetoothLE.Hosting;

namespace Sample.BlePerf;


public class ServerViewModel : ViewModel
{
    readonly IBleHostingManager hostingManager;
    readonly Subject<byte[]> packetSubj = new();


    public ServerViewModel(BaseServices services, IBleHostingManager hostingManager) : base(services)
    {
        this.hostingManager = hostingManager;

        this.ToggleGatt = this.CreateCommand(
            async () =>
            {
                await hostingManager.AddService(
                    Constants.ServiceUuid,
                    true,
                    sb => sb.AddCharacteristic(
                        Constants.CharacteristicUuid,
                        cb => cb.SetWrite(
                            request =>
                            {
                                this.Mtu = request.Peripheral.Mtu;
                                this.packetSubj.OnNext(request.Data);
                                return Task.FromResult(GattState.Success);
                            },
                            this.WriteWithResponse ? WriteOptions.Write : WriteOptions.WriteWithoutResponse
                        )
                    )
                );
                return Disposable.Create(() => hostingManager.ClearServices());
            },
            "GATT"
        );

        this.ToggleL2Cap = this.CreateCommand(
            async () =>
            {
                var instance = await this.hostingManager.OpenL2Cap(this.Secure, channel =>
                    channel.DataReceived.Subscribe(x => this.packetSubj.OnNext(x)
                ));

                this.Psm = instance.Psm;
                return instance;
            },
            "L2CAP"
        );
    }

    [Reactive] public bool WriteWithResponse { get; set; } = true;
    [Reactive] public bool Secure { get; set; }
    [Reactive] public int Psm { get; private set; }

    [Reactive] public bool IsBroadcasting { get; private set; }
    [Reactive] public string BroadcastType { get; private set; }
    [Reactive] public string TransferSpeed { get; private set; }
    [Reactive] public long BytesReceived { get; private set; }
    [Reactive] public long PacketCount { get; private set; }
    [Reactive] public int Mtu { get; private set; }

    public ICommand ToggleL2Cap { get; }
    public ICommand ToggleGatt { get; }
    public override void OnNavigatedFrom(INavigationParameters parameters) => this.Stop();


    CompositeDisposable? cleanup;
    ICommand CreateCommand(Func<Task<IDisposable>> setup, string type) => ReactiveCommand
        .CreateFromTask(async () =>
        {
            if (this.IsBroadcasting)
            {
                this.Stop();
            }
            else
            {
                this.cleanup = new();
                var disp = await setup.Invoke();
                this.cleanup.Add(disp);

                await this.Broadcast(type);
            }
        },
        this.WhenAny(
            x => x.IsBroadcasting,
            x => x.BroadcastType,
            (bc, t) => !bc.GetValue() || t.GetValue().Equals(type)
        )
    );


    async Task Broadcast(string type)
    {
        this.PacketCount = 0;
        this.BytesReceived = 0;
        this.Mtu = 0;
        this.TransferSpeed = String.Empty;

        this.BroadcastType = type;
        this.IsBroadcasting = true;

        this.packetSubj
            .Buffer(TimeSpan.FromSeconds(2))
            .SubOnMainThread(packets =>
            {
                if (packets.Count == 0)
                {
                    this.TransferSpeed = "0.0 bp/s";
                }
                else
                {
                    var bytesReceived = packets.Sum(x => x.Length);

                    this.BytesReceived += bytesReceived;
                    this.PacketCount += packets.Count;

                    // bytes for the last 2 seconds
                    this.TransferSpeed = (bytesReceived / 2).Bytes().ToString("#.#") + "/s";
                }
            })
            .DisposedBy(this.cleanup!);

        await this.hostingManager.StartAdvertising(new(
            "BLEPERF",
            Constants.ServiceUuid
        ));
    }


    void Stop()
    {
        this.cleanup?.Dispose();
        this.IsBroadcasting = false;
        
        this.hostingManager.StopAdvertising();
    }
}

