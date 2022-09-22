using System.Text;
using Shiny.BluetoothLE;

namespace Sample.BleClient;


public class L2CapViewModel : ViewModel
{
    IPeripheral peripheral = null!;
    L2CapChannel? channel;
    

    public L2CapViewModel(BaseServices services) : base(services)
    {
        this.Write = ReactiveCommand.CreateFromTask(
            async () =>
            {
                var bytes = Encoding.UTF8.GetBytes(this.WriteValue!);
                await this.channel!.Write(bytes);

                this.Output = $"{this.WriteValue}{Environment.NewLine}{this.Output}";
                this.WriteValue = String.Empty;
            },
            this.WhenAny(
                x => x.IsConnected,
                x => x.WriteValue,
                (conn, wv) => conn.GetValue() && !wv.GetValue().IsEmpty()
            )
        );

        this.Toggle = ReactiveCommand.CreateFromTask(
            async () =>
            {
                if (this.IsConnected)
                {
                    await this.peripheral.WithConnectIf();

                    this.channel = await this.peripheral
                        .TryOpenL2CapChannel(
                            (ushort)this.Psm,
                            this.Secure
                        )
                        .ToTask();

                    if (this.channel == null)
                        throw new InvalidOperationException("Could not open L2Cap channel");

                    this.channel
                        .DataReceived
                        .SubOnMainThread(data =>
                        {
                            var value = Encoding.UTF8.GetString(data.ToArray());
                            this.Input = $"{value}{Environment.NewLine}{this.Input}";
                        });

                    this.IsConnected = true;
                }
                else
                {
                    this.IsConnected = false;
                    this.channel?.Dispose();
                    this.channel = null;
                    this.peripheral.CancelConnection();
                }
            },
            this.WhenAny(
                x => x.Psm,
                x => x.GetValue() > 0 && x.GetValue() <= ushort.MaxValue
            )
        );
    }


    [Reactive] public int Psm { get; set; }
    [Reactive] public bool Secure { get; set; }
    [Reactive] public bool IsConnected { get; private set; }
    [Reactive] public string Output { get; private set; }
    [Reactive] public string Input { get; private set; }
    [Reactive] public string WriteValue { get; private set; }
    public ICommand Write { get; }
    public ICommand Toggle { get; }


    public override Task InitializeAsync(INavigationParameters parameters)
    {
        this.peripheral = parameters.GetValue<IPeripheral>("Peripheral");
        return base.InitializeAsync(parameters);
    }
}
