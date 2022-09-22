using Shiny.BluetoothLE;

namespace Sample.BlePerf;


public class ClientViewModel : ViewModel
{
    public ClientViewModel(BaseServices services, IBleManager bleManager) : base(services)
    {
        this.ToggleGatt = ReactiveCommand.CreateFromTask(async () =>
        {
            var peripheral = await this.FindDevice(bleManager);
        });

        this.ToggleL2Cap = ReactiveCommand.CreateFromTask(async () =>
        {
            var peripheral = await this.FindDevice(bleManager);

        });
    }

    [Reactive] public string Speed { get; private set; }
    [Reactive] public long BytesSent { get; private set; }
    [Reactive] public long PacketCount { get; private set; }
    // TODO: MTU
    public ICommand ToggleGatt { get; }
    public ICommand ToggleL2Cap { get; }


    Task<IPeripheral> FindDevice(IBleManager manager) => manager
        .Scan()
        .Take(1)
        .Timeout(TimeSpan.FromSeconds(30))
        .Select(x => x.Peripheral)
        .Select(x => x.WithConnectIf())
        .Switch()
        .ToTask();
}

