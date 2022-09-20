using System.Text;
using Shiny.BluetoothLE;

namespace Sample.BleClient;


public class L2CapViewModel : ViewModel
{
    IPeripheral peripheral = null!;
    L2CapChannel? channel;


    public L2CapViewModel(BaseServices services) : base(services)
    {
        this.Toggle = ReactiveCommand.CreateFromTask(async () =>
        {
            if (this.IsConnected)
            {
                await this.peripheral.WithConnectIf();

                this.channel = await this.peripheral.TryOpenL2CapChannel(
                    (ushort)this.Psm,
                    this.Secure
                );
                if (this.channel == null)
                    throw new InvalidOperationException("Could not open L2Cap channel");

                Task.Run(async () =>
                {
                    var buffer = new Memory<byte>();
                    await this.channel.InputStream.ReadAsync(buffer, CancellationToken.None);

                    this.Input = Encoding.UTF8.GetString(buffer.ToArray()) + Environment.NewLine + this.Input;
                });

                this.IsConnected = true;
                // TODO: start reader thread
            }
            else
            {
  
            }
        });
    }


    [Reactive] public int Psm { get; set; }
    [Reactive] public bool Secure { get; set; }
    [Reactive] public bool IsConnected { get; private set; }
    [Reactive] public string Output { get; set; }
    [Reactive] public string Input { get; private set; }
    public ICommand Toggle { get; }


    public override Task InitializeAsync(INavigationParameters parameters)
    {
        this.peripheral = parameters.GetValue<IPeripheral>("Peripheral");

        return base.InitializeAsync(parameters);
    }
}
