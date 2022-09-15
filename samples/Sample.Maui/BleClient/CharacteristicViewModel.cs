using System.Text;
using Shiny.BluetoothLE;

namespace Sample.BleClient;


public class CharacteristicViewModel : ViewModel
{
    IDisposable? dispose;


    public CharacteristicViewModel(IGattCharacteristic characteristic)
    {
        this.ServiceUUID = characteristic.Service.Uuid;
        this.UUID = characteristic.Uuid;

        this.CanNotify = characteristic.CanNotify();
        this.CanRead = characteristic.CanRead();
        this.CanWrite = characteristic.CanWrite();

        this.Read = this.LoadingCommand(async () =>
        {
            var result = await characteristic.ReadAsync();
            this.SetRead(result.Data);
        });


        this.ToggleNotify = ReactiveCommand.Create(() =>
        {
            if (this.dispose == null)
            {
                this.dispose = characteristic
                    .Notify()
                    .SubOnMainThread(x => this.SetRead(x.Data));
            }
            else
            {
                this.Stop();
            }
        });
    }


    //public override void OnDisappearing()
    //{
    //    base.OnDisappearing();
    //    this.Stop();
    //}


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


    public bool CanRead { get; }
    public bool CanWrite { get; }
    public bool CanNotify { get; }
    public string ServiceUUID { get; }
    public string UUID { get; }

    public ICommand Read { get; }
    public ICommand Write { get; }
    public ICommand ToggleNotify { get; }

    
    [Reactive] public bool IsNotifying { get; private set; }
    [Reactive] public string ReadValue { get; private set; }
    [Reactive] public string WriteValue { get; private set; }
    [Reactive[ public string LastValueTime { get; private set; }
}
