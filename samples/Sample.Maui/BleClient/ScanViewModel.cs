using Shiny;
using Shiny.BluetoothLE;

namespace Sample.BleClient;


public class ScanViewModel : ViewModel
{
    IDisposable? scanSub;


    public ScanViewModel(BaseServices services, IBleManager bleManager) : base(services)
    {
        this.IsScanning = bleManager?.IsScanning ?? false;

        this.WhenAnyValueSelected(x => x.SelectedPeripheral, async x =>
        {
            this.StopScan();
            await this.Navigation.Navigate("BlePeripheral", ("Peripheral", x.Peripheral));
        });

        this.ScanToggle = ReactiveCommand.CreateFromTask(
            async () =>
            {
                if (bleManager == null)
                {
                    await this.Alert("Platform Not Supported");
                    return;
                }
                if (this.IsScanning)
                {
                    this.StopScan();
                }
                else
                {
                    this.Peripherals.Clear();
                    this.IsScanning = true;

                    this.scanSub = bleManager
                        .Scan()
                        .Buffer(TimeSpan.FromSeconds(1))
                        .Where(x => x?.Any() ?? false)
                        .SubOnMainThread(
                            results =>
                            {
                                var list = new List<PeripheralItemViewModel>();
                                foreach (var result in results)
                                {
                                    var peripheral = this.Peripherals.FirstOrDefault(x => x.Equals(result.Peripheral));
                                    if (peripheral == null)
                                        peripheral = list.FirstOrDefault(x => x.Equals(result.Peripheral));

                                    if (peripheral != null)
                                    {
                                        peripheral.Update(result);
                                    }
                                    else
                                    {
                                        peripheral = new PeripheralItemViewModel(result.Peripheral);
                                        peripheral.Update(result);
                                        list.Add(peripheral);
                                    }
                                }
                                if (list.Any())
                                {
                                    // XF is not able to deal with an observablelist/addrange properly
                                    foreach (var item in list)
                                        this.Peripherals.Add(item);
                                }
                            },
                            ex => this.Alert(ex.ToString(), "ERROR")
                        );
                }
            }
        );
    }


    public ICommand NavToTest { get; }
    public ICommand ScanToggle { get; }
    public ObservableCollection<PeripheralItemViewModel> Peripherals { get; } = new();

    [Reactive] public PeripheralItemViewModel? SelectedPeripheral { get; set; }
    [Reactive] public bool IsScanning { get; private set; }


    void StopScan()
    {
        this.scanSub?.Dispose();
        this.scanSub = null;
        this.IsScanning = false;
    }
}