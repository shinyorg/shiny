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


namespace Samples.BluetoothLE
{
    public class ManagedScanViewModel : ViewModel
    {
        readonly IManagedScan scanner;


        public ManagedScanViewModel(IBleManager bleManager, INavigationService navigator)
        {
            this.scanner = bleManager
                .CreateManagedScanner(RxApp.MainThreadScheduler, TimeSpan.FromSeconds(10))
                .DisposedBy(this.DeactivateWith);

            this.Toggle = ReactiveCommand.CreateFromTask(async () =>
                this.IsBusy = await this.scanner.Toggle()
            );

            this.WhenAnyValue(x => x.SelectedPeripheral)
                .Skip(1)
                .Where(x => x != null)
                .Subscribe(async x =>
                {
                    this.SelectedPeripheral = null;
                    this.scanner.Stop();
                    await navigator.Navigate("ManagedPeripheral", ("Peripheral", x.Peripheral));
                });
        }


        public ICommand Toggle { get;  }
        [Reactive] public ManagedScanResult? SelectedPeripheral { get; set; }
        public ObservableCollection<ManagedScanResult> Peripherals
            => this.scanner.Peripherals;
    }
}

