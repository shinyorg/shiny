using Shiny;
using Shiny.BluetoothLE;

namespace Sample.BleClient;


public class PeripheralViewModel : ViewModel
{
    public PeripheralViewModel(BaseServices services, IPeripheral peripheral) : base(services)
    {
        this.Title = peripheral.Name;

        //this.Load = this.LoadingCommand(async () =>
        //{
        //    this.Services = (await peripheral.GetServicesAsync())
        //        .Select(x => new ServiceViewModel(x))
        //        .ToList();

        //    this.RaisePropertyChanged(nameof(this.Services));
        //});

        //this.WhenAnyProperty(x => x.SelectedService)
        //    .Where(x => x != null)
        //    .SubOnMainThread(async x =>
        //    {
        //        this.SelectedService = null;
        //        await this.Navigation.PushAsync(new ServicePage
        //        {
        //            BindingContext = x
        //        });
        //    });
    }


    public string Title { get; }
    public ICommand Load { get; }
    public List<ServiceViewModel> Services { get; private set; }

    [Reactive] public ServiceViewModel SelectedService { get; set; }


    public override Task InitializeAsync(INavigationParameters parameters)
    {
        this.Load.Execute(null);
        return base.InitializeAsync(parameters);
    }
}
