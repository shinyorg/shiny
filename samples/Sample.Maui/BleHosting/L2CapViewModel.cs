using System.Text;
using Shiny.BluetoothLE.Hosting;

namespace Sample.BleHosting;


public class L2CapViewModel : ViewModel
{
    L2CapInstance? instance;
    L2CapChannel? channel;
    Task readerTask = null!;


    public L2CapViewModel(BaseServices services, IBleHostingManager hostingManager) : base(services)
    {
        this.Toggle = ReactiveCommand.CreateFromTask(async () =>
        {
            if (this.IsBroadcasting)
            {
                this.Stop();
            }
            else
            {
                // TODO: multiple channels can connect
                this.instance = await hostingManager.OpenL2Cap(this.Secure, channel =>
                {
                    this.channel = channel;
                    this.IsConnected = true;

                    this.channel
                        .DataReceived
                        .SubOnMainThread(data =>
                        {
                            this.IsConnected = true;
                            var value = Encoding.UTF8.GetString(data);
                            this.Input = $"{value}{Environment.NewLine}{this.Input}";
                        });
                });
                this.Psm = this.instance.Value.Psm;
                this.IsBroadcasting = true;
            }
        });

        this.Write = ReactiveCommand.CreateFromTask(
            async () =>
            {
                var data = Encoding.UTF8.GetBytes(this.WriteValue!);
                await this.channel!.Write(data).ToTask();

                this.Output = $"{this.WriteValue}{Environment.NewLine}{this.Output}";
                this.WriteValue = String.Empty;
            },
            this.WhenAny(
                x => x.IsConnected,
                x => x.WriteValue,
                (conn, wv) => conn.GetValue() && !wv.GetValue().IsEmpty()
            )
        );
    }


    public ICommand Toggle { get; }
    public ICommand Write { get; }
    
    [Reactive] public string WriteValue { get; set; }
    [Reactive] public string Input { get; private set; }
    [Reactive] public bool IsBroadcasting { get; private set; }
    [Reactive] public bool IsConnected { get; private set; }
    [Reactive] public bool Secure { get; set; }
    [Reactive] public string Output { get; private set; }
    [Reactive] public int Psm { get; private set; }

    public override void OnNavigatedFrom(INavigationParameters parameters) => this.Stop();
    void Stop()
    {
        this.instance?.Dispose();
        this.instance = null;
        this.IsConnected = false;
        this.IsBroadcasting = false;
        this.Psm = 0;
    }
}
