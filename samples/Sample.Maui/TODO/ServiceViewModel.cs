using Shiny;
using Shiny.BluetoothLE;

namespace Sample.BleClient;


public class ServiceViewModel : ViewModel
{
    IGattService service;


    public ServiceViewModel(BaseServices services) : base(services)
    {
        // TODO
        this.Load = ReactiveCommand.CreateFromTask(async () =>
        {
            //this.Characteristics = (await this.service!.GetCharacteristicsAsync())
            //    .Select(x => new CharacteristicViewModel(x))
            //    .ToList();

            this.RaisePropertyChanged(nameof(this.Characteristics));
        });

        this.WhenAnyValueSelected(x => x.SelectedCharacteristic, async x =>
        {
            await this.Navigation.Navigate(nameof(CharacteristicPage), ("Characteristic", x));
        });
    }


    public string Title { get; }
    public ICommand Load { get; }
    public List<CharacteristicViewModel> Characteristics { get; private set; }
    [Reactive] public CharacteristicViewModel SelectedCharacteristic { get; set; }

    public override Task InitializeAsync(INavigationParameters parameters)
    {
        this.service = parameters.GetValue<IGattService>("Service");
        this.Load.Execute(null);
        //this.Title = service.Uuid;

        return base.InitializeAsync(parameters);
    }
}
