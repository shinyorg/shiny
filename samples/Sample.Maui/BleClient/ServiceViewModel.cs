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
            //this.Characteristics = (await this.peripheral!.GetCharacteristicsAsync(this.service.Uuid))
            //    .Select(x => new CharacteristicViewModel(x))
            //    .ToList();
        });

        this.WhenAnyValueSelected(
            x => x.SelectedCharacteristic,
            async x => await this.Navigation.Navigate(nameof(CharacteristicPage), ("Characteristic", x))
        );
    }



    public ICommand Load { get; }
    [Reactive] public List<CharacteristicViewModel> Characteristics { get; private set; }
    [Reactive] public CharacteristicViewModel? SelectedCharacteristic { get; set; }

    public override Task InitializeAsync(INavigationParameters parameters)
    {
        this.service = parameters.GetValue<BleServiceInfo>("Service");
        this.Load.Execute(null);
        this.Title = this.service.Uuid;

        return base.InitializeAsync(parameters);
    }
}
