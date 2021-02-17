using System;
using System.Collections.ObjectModel;
using System.Reactive.Linq;
using System.Windows.Input;
using Prism.Navigation;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Shiny.BluetoothLE;


namespace Samples.BluetoothLE
{
    public class ManagedScanViewModel : ViewModel
    {
        readonly ManagedScan scanner;


        public ManagedScanViewModel(IBleManager bleManager, INavigationService navigator)
        {
            this.scanner = bleManager.CreateManagedScanner();
            this.scanner.Scheduler = RxApp.MainThreadScheduler;

            this.Toggle = ReactiveCommand.Create(() =>
            {
                scanner.Toggle();
                this.IsBusy = scanner.IsScanning;
            });

            this.WhenAnyValue(x => x.SelectedPeripheral)
                .Skip(1)
                .Where(x => x != null)
                .Subscribe(async x =>
                {
                    scanner.Stop();
                    await navigator.Navigate("Peripheral", ("Peripheral", x.Peripheral));
                });
        }


        public ICommand Toggle { get;  }
        [Reactive] public ManagedScanPeripheral SelectedPeripheral { get; set; }
        public ObservableCollection<ManagedScanPeripheral> Peripherals
            => this.scanner.Peripherals;
    }
}

