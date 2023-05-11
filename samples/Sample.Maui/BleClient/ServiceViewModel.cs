using Shiny.BluetoothLE;

namespace Sample.BleClient;


public class ServiceViewModel : ViewModel
{
    IPeripheral peripheral = null!;
    BleServiceInfo service = null!;

    public ServiceViewModel(BaseServices services) : base(services)
    {
        this.Load = ReactiveCommand.CreateFromTask(async () =>
        {
            this.IsBusy = true;
            await this.peripheral.ConnectAsync();
            this.Characteristics = (await this.peripheral!.GetCharacteristicsAsync(this.service.Uuid)).ToList();
            this.IsBusy = false;
        });

        this.WhenAnyValueSelected(
            x => x.SelectedCharacteristic,
            x => this.Navigation.Navigate(
                "BlePeripheralCharacteristic",
                ("Peripheral", this.peripheral),
                ("Characteristic", x!)
            )
        );
    }



    public ICommand Load { get; }
    [Reactive] public List<BleCharacteristicInfo> Characteristics { get; private set; }
    [Reactive] public BleCharacteristicInfo? SelectedCharacteristic { get; set; }


    public override void OnAppearing() => this.Load.Execute(null);
    public override Task InitializeAsync(INavigationParameters parameters)
    {
        this.service = parameters.GetValue<BleServiceInfo>("Service");
        this.peripheral = parameters.GetValue<IPeripheral>("Peripheral");
        this.Title = $"{this.peripheral.Name} - {this.service.Uuid}";

        return Task.CompletedTask;
    }
}