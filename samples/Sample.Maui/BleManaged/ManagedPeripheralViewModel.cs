using System.Text;
using Shiny;
using Shiny.BluetoothLE;
using Shiny.BluetoothLE.Managed;

namespace Sample.BleManaged;


public class ManagedPeripheralViewModel : ViewModel
{
    const string ServiceUuid = "FFF0";
    const string RxCharacteristicUuid = "FFF1";
    const string TxCharacteristicUuid = "FFF2";
   

    public ManagedPeripheralViewModel(BaseServices services) : base(services)
    {
        this.ToggleRssi = ReactiveCommand.Create(() =>
            this.IsRssi = this.Peripheral?.ToggleRssi() ?? false
        );
        this.Start = ReactiveCommand.CreateFromTask(
            async () =>
            {
                this.IsStarted = true;
                this.Peripheral = this.peripheral.CreateManaged();

                var sub = this.Peripheral
                    .WhenNotificationReceived(ServiceUuid, RxCharacteristicUuid)
                    .SubOnMainThread(data =>
                    {
                        var txt = Encoding.ASCII.GetString(data);
                        this.WriteLog("[RECV]: " + txt);
                    });

                await this.Write("ATZ");
                await this.Write("AT RV");
                await this.Write("ATI");
                await this.Write("AT PC");
                await this.Write("AT D");
                await this.Write("AT E0");
                await this.Write("AT SP 5");
                await this.Write("AT ST 80");
                await this.Write("AT SH 81 10 F0");
                await this.Write("21 0B 01");

                while (!this.CancelToken.IsCancellationRequested)
                {
                    await this.Write("0100");
                    await Task.Delay(2000);
                }
                sub.Dispose();
                this.IsStarted = false;
            },
            this.WhenAny(
                x => x.IsStarted,
                x => !x.GetValue()
            )
        );

        this.Stop = ReactiveCommand.Create(
            () => this.DoStop(),
            this.WhenAny(
                x => x.IsStarted,
                x => x.GetValue()
            )
        );
    }


    public override void OnNavigatedTo(INavigationParameters parameters)
    {
        base.OnNavigatedTo(parameters);
        this.peripheral = parameters.GetValue<IPeripheral>("Peripheral");
    }


    //public override void OnDisappearing()
    //{
    //    base.OnDisappearing();
    //    this.DoStop();
    //}


    IPeripheral? peripheral;
    public ICommand ToggleRssi { get; }
    public IManagedPeripheral? Peripheral { get; set; }

    public ICommand Start { get; }
    public ICommand Stop { get; }
    [Reactive] public bool IsStarted { get; private set; }
    [Reactive] public bool IsRssi { get; private set; }
    [Reactive] public string Log { get; private set; }


    void DoStop()
    {
        // TODO
        this.Peripheral?.Dispose();
        //this.Deactivate();
        this.IsStarted = false;
    }


    void WriteLog(string value)
        => this.Log = value + Environment.NewLine + this.Log;


    async Task Write(string cmd)
    {
        var bytes = Encoding.ASCII.GetBytes(cmd + '\r');
        this.WriteLog("[SENDING]: " + cmd);
        await this.Peripheral!.Write(ServiceUuid, TxCharacteristicUuid, bytes, true);
        this.WriteLog("[SENT]: " + cmd);
    }
}
