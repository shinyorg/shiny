using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using Prism.Navigation;
using ReactiveUI;
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
            //this.NavToPeripheral = navigator.NavigateCommand<ManagedScanPeripheral>(
            //    "",
            //    p => p.Peripheral
            //);
        }


        public ICommand Toggle { get;  }
        public ICommand NavToPeripheral { get; }
        public ObservableCollection<ManagedScanPeripheral> Peripherals
            => this.scanner.Peripherals;
    }
}

