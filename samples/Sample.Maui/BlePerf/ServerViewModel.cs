using Shiny.BluetoothLE.Hosting;

namespace Sample.BlePerf;


public class ServerViewModel : ViewModel
{
    public ServerViewModel(BaseServices services, IBleHostingManager hostingManager) : base(services)
    {
        this.ToggleGatt = ReactiveCommand.CreateFromTask(async () =>
        {

        });

        this.ToggleL2Cap = ReactiveCommand.CreateFromTask(async () =>
        {

        });
    }


    [Reactive] public string TransferSpeed { get; private set; }
    [Reactive] public long BytesReceived { get; private set; }
    [Reactive] public long PacketCount { get; private set; }
    // TODO: MTU?
    public ICommand ToggleL2Cap { get; }
    public ICommand ToggleGatt { get; }
}

