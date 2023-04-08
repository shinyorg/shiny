using Shiny.BluetoothLE;

namespace Sample.BleClient;


public class PeripheralViewModel : ViewModel
{
    IPeripheral peripheral = null!;

    public PeripheralViewModel(BaseServices services) : base(services)
    {
        this.Load = ReactiveCommand.CreateFromTask(async () =>
        {
            var services = await this.peripheral
                .WithConnectIf()
                .Select(x => x.GetServices())
                .Switch()
                .ToTask();

            this.Services = services.ToList();
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

        this.WhenAnyValueSelected(
            x => x.SelectedService,
            x => this.Navigation.Navigate(
                "BlePeripheralService",
                ("Peripheral", this.peripheral),
                ("Service", x!)
            )
        );
    }

    public ICommand Load { get; }
    public ICommand GetDeviceInfo { get; }

    [Reactive] public IList<BleServiceInfo> Services { get; private set; } = null!;
    [Reactive] public BleServiceInfo? SelectedService { get; set; }


    public override void OnNavigatedTo(INavigationParameters parameters)
    {
        this.peripheral = parameters.GetValue<IPeripheral>("Peripheral")!;
        this.Title = this.peripheral.Name ?? "No Name";

        this.Load.Execute(null);
    }
}
