using System.Text;
using Shiny.BluetoothLE;

namespace Sample.BleClient;


public class CharacteristicViewModel : ViewModel
{
    IPeripheral peripheral = null!;
    BleCharacteristicInfo characteristic = null!;
    IDisposable? dispose;


    public CharacteristicViewModel(BaseServices services) : base(services)
    {
        this.Read = ReactiveCommand.CreateFromTask(
            async () =>
            {
                var result = await this.peripheral!.ReadCharacteristicAsync(this.characteristic);
                this.Set(result);
            },
            this.WhenAny(x => x.CanRead, x => x.GetValue())
        );

        this.Write = ReactiveCommand.CreateFromTask(
            async () =>
            {
                var value = await this.Dialogs.DisplayPromptAsync("Data", "Enter some data that we will encode to ship to device", "OK", "Cancel");
                if (!value.IsEmpty())
                {
                    var data = Encoding.UTF8.GetBytes(value);
                    var result = await this.peripheral.WriteCharacteristicAsync(this.characteristic, data);
                    this.Set(result);
                }
            },
            this.WhenAny(x => x.CanWrite, x => x.GetValue())
        );

        this.ToggleNotify = ReactiveCommand.Create(
            () =>
            {
                if (this.dispose == null)
                {
                    this.dispose = this.peripheral
                        .NotifyCharacteristic(this.characteristic)
                        .SubOnMainThread(x => this.Set(x));
                }
                else
                {
                    this.Stop();
                }
            },
            this.WhenAny(x => x.CanNotify, x => x.GetValue())
        );
    }


    public override Task InitializeAsync(INavigationParameters parameters)
    {
        this.peripheral = parameters.GetValue<IPeripheral>("Peripheral");
        this.characteristic = parameters.GetValue<BleCharacteristicInfo>("Characteristic");
        this.ServiceUUID = this.characteristic.Service.Uuid;
        this.UUID = this.characteristic.Uuid;

        this.CanNotify = this.characteristic.CanNotify();
        this.CanRead = this.characteristic.CanRead();
        this.CanWrite = this.characteristic.CanWrite();
        return Task.CompletedTask;
    }


    public override void OnDisappearing() => this.Stop();


    void Stop()
    {
        this.dispose?.Dispose();
        this.dispose = null;
        this.IsNotifying = false;
    }


    void Set(BleCharacteristicResult result)
    {
        this.LastEvent = result.Event.ToString();
        this.LastValue = result.Data == null ? "NO DATA" : Encoding.UTF8.GetString(result.Data);
        this.LastValueTime = DateTime.Now.ToString();
    }


    public ICommand Read { get; }
    public ICommand Write { get; }
    public ICommand ToggleNotify { get; }

    [Reactive] public bool CanRead { get; private set; }
    [Reactive] public bool CanWrite { get; private set; }
    [Reactive] public bool CanNotify { get; private set; }
    [Reactive] public string ServiceUUID { get; private set; }
    [Reactive] public string UUID { get; private set; }
    [Reactive] public bool IsNotifying { get; private set; }
    [Reactive] public string LastEvent { get; private set; } = "None";
    [Reactive] public string LastValue { get; private set; }
    [Reactive] public string LastValueTime { get; private set; }
}
