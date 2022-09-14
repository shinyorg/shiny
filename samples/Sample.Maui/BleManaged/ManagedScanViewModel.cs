using System;
using System.Collections.ObjectModel;
using System.Reactive.Linq;
using System.Windows.Input;
using Prism.Navigation;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Shiny;
using Shiny.BluetoothLE;
using Shiny.BluetoothLE.Managed;
using Xamarin.Forms;


namespace Sample
{
    public class ManagedScanViewModel : ViewModel
    {
        readonly IManagedScan scanner;


        public ManagedScanViewModel(IBleManager bleManager, INavigationService navigator)
        {
            // we are specifically scanning for VeePeak BLE OBD device
            this.scanner = bleManager
                .CreateManagedScanner(
                    RxApp.MainThreadScheduler,
                    TimeSpan.FromSeconds(10),
                    new ScanConfig
                    {
                        ServiceUuids =
                        {
                            "FFF0"
                        }
                    }
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
                    await navigator.Navigate(
                        nameof(ManagedPeripheralPage),
                        ("Peripheral", x!.Peripheral)
                    );
                }
            );
        }


        public ICommand Toggle { get; }
        public ObservableCollection<ManagedScanResult> Peripherals => this.scanner.Peripherals;
        [Reactive] public ManagedScanResult? SelectedPeripheral { get; set; }


        public override void OnDisappearing()
        {
            base.OnDisappearing();
            this.scanner.Stop();
        }
    }
}

