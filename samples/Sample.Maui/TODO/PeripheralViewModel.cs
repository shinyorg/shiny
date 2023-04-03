using Shiny;
using Shiny.BluetoothLE;

namespace Sample.BleClient;


public class PeripheralViewModel : ViewModel
{
    public PeripheralViewModel(BaseServices services) : base(services)
    {
        
        this.Load = ReactiveCommand.CreateFromTask(async () =>
        {
            //this.peripheral
            //    .WithConnectIf()
            //    .Select(x => x.GetServices())
            //    .Switch()
            //    .ToTask();
            //this.Services = (await peripheral.GetServicesAsync())
            //    .Select(x => new ServiceViewModel(x))
            //    .ToList();

            this.RaisePropertyChanged(nameof(this.Services));
        });
        

        this.GetDeviceInfo = ReactiveCommand.CreateFromTask(async () =>
        {
            this.IsBusy = true;
            var result = await this.peripheral
                .WithConnectIf()
                .Select(x => x.ReadDeviceInformation())
                .Switch()
                .Timeout(TimeSpan.FromSeconds(10))
                .Finally(() =>
                {
                    this.IsBusy = false;
                    this.peripheral.CancelConnection();
                })
                .ToTask();

            await this.Dialogs.DisplayAlertAsync(
                "Device Info",
                $"Manufacturer: {result?.ManufacturerName}\r\nModel: {result?.ModelNumber}\r\nHW:{result?.HardwareRevision}\r\nSW: {result?.SoftwareRevision}\r\nSN: {result?.SerialNumber}\r\nFirmware: {result?.FirmwareRevision}",
                "OK"
            );
        });

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


    IPeripheral peripheral = null!;
    
    public ICommand Load { get; }
    public ICommand GetDeviceInfo { get; }    
    public List<ServiceViewModel> Services { get; private set; }

    [Reactive] public ServiceViewModel SelectedService { get; set; }


    public override Task InitializeAsync(INavigationParameters parameters)
    {
        this.peripheral = parameters.GetValue<IPeripheral>("Peripheral");
        //this.Title = peripheral.Name;
        this.Load.Execute(null);
        return base.InitializeAsync(parameters);
    }
}
