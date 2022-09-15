using Shiny.BluetoothLE;
using Shiny.BluetoothLE.Managed;

namespace Sample.BleManaged;


public class ManagedScanViewModel : ViewModel
{
    readonly IManagedScan scanner;


    public ManagedScanViewModel(BaseServices services, IBleManager bleManager) : base(services)
    {
        // we are specifically scanning for VeePeak BLE OBD device
        this.scanner = bleManager
            .CreateManagedScanner(
                RxApp.MainThreadScheduler,
                TimeSpan.FromSeconds(10),
                new ScanConfig(BleScanType.Balanced, false, "FFF0")
            )
            .DisposedBy(this.DestroyWith);

        this.Toggle = ReactiveCommand.Create(async () =>
            this.IsBusy = await this.scanner.Toggle()
        );

        this.WhenAnyValueSelected(
            x => x.SelectedPeripheral,
            async x =>
            {
                this.scanner.Stop();
                await this.Navigation.Navigate(
                    nameof(ManagedPeripheralPage),
                    ("Peripheral", x!.Peripheral)
                );
            }
        );
    }


    public ICommand Toggle { get; }
    public ObservableCollection<ManagedScanResult> Peripherals => this.scanner.Peripherals;
    [Reactive] public ManagedScanResult? SelectedPeripheral { get; set; }
}

