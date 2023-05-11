using Shiny.BluetoothLE;
using Shiny.BluetoothLE.Managed;

namespace Sample.BleManaged;


public class ManagedScanViewModel : ViewModel
{
    readonly IManagedScan scanner;


    public ManagedScanViewModel(
        BaseServices services,
        IBleManager bleManager,
        ILogger<ManagedScanViewModel> logger
    ) : base(services)
    {
        // we are specifically scanning for VeePeak BLE OBD device
        this.scanner = bleManager
            .CreateManagedScanner()
            .DisposedBy(this.DestroyWith);

        this.Toggle = ReactiveCommand.Create(async () =>
        {
            if (this.IsBusy)
            {
                this.scanner.Stop();
                logger.LogTrace("Stop Scan");
            }
            else
            {
                logger.LogTrace("Start Scan");
                await this.scanner.Start(
#if ANDROID
                    new AndroidScanConfig(
                        Android.Bluetooth.LE.ScanMode.Opportunistic,
                        true,
                        null
                    ),
#else
                    new ScanConfig(
                    ),
#endif
                    x =>
                    {
                        // optional predicate filter
                        var found = true;
                        var pName = (x.Peripheral.Name ?? "None");
                        logger.LogTrace("Peripheral Name: " + pName);
                        return found;
                    },
                    RxApp.MainThreadScheduler,
                    null, // buffer time - default is 3 seconds
                    TimeSpan.FromSeconds(10)
                );
            }
            this.IsBusy = this.scanner.IsScanning;
        });

        this.WhenAnyValueSelected(
            x => x.SelectedPeripheral,
            async x =>
            {
                this.scanner.Stop();
                await this.Navigation.Navigate(
                    "BleManagedPeripheral",
                    ("Peripheral", x!.Peripheral)
                );
            }
        );
    }


    public ICommand Toggle { get; }
    public INotifyReadOnlyCollection<ManagedScanResult> Peripherals => this.scanner.Peripherals;
    [Reactive] public ManagedScanResult? SelectedPeripheral { get; set; }
}

