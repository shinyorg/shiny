using System.Text;
using Shiny.BluetoothLE;

namespace Sample.BleClient;


public class CharacteristicViewModel : ViewModel
{
    IGattCharacteristic characteristic = null!;
    IDisposable? dispose;


    public CharacteristicViewModel(BaseServices services, IGattCharacteristic characteristic) : base(services)
    {
        this.Read = ReactiveCommand.CreateFromTask(async () =>
        {
            var result = await this.characteristic!.ReadAsync();
            this.SetRead(result.Data!);
        });


        this.ToggleNotify = ReactiveCommand.Create(() =>
        {
            if (this.dispose == null)
            {
                this.dispose = this.characteristic
                    .Notify()
                    .SubOnMainThread(x => this.SetRead(x.Data!));
            }
            else
            {
                this.Stop();
            }
        });
    }


    public override Task InitializeAsync(INavigationParameters parameters)
    {
        this.characteristic = parameters.GetValue<IGattCharacteristic>("Characteristic");
        this.ServiceUUID = this.characteristic.Service.Uuid;
        this.UUID = this.characteristic.Uuid;

        this.CanNotify = this.characteristic.CanNotify();
        this.CanRead = this.characteristic.CanRead();
        this.CanWrite = this.characteristic.CanWrite();
        return base.InitializeAsync(parameters);
    }



    void Stop()
    {
        this.dispose?.Dispose();
        this.dispose = null;
        this.IsNotifying = false;
    }


    void SetRead(byte[] data)
    {
        this.ReadValue = Encoding.UTF8.GetString(data);
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
    [Reactive] public string ReadValue { get; private set; }
    [Reactive] public string WriteValue { get; private set; }
    [Reactive] public string LastValueTime { get; private set; }
}
