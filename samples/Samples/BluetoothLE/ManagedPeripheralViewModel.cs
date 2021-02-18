using System;
using System.Windows.Input;
using Prism.Navigation;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Shiny.BluetoothLE;
using Shiny.BluetoothLE.Managed;


namespace Samples.BluetoothLE
{
    public class ManagedPeripheralViewModel : ViewModel
    {
        public ManagedPeripheralViewModel()
        {
            this.ToggleRssi = ReactiveCommand.Create(() =>
                this.IsRssi = this.Peripheral.ToggleRssi()
            );
        }

        public override void OnNavigatedTo(INavigationParameters parameters)
        {
            this.Peripheral = parameters
                .GetValue<IPeripheral>("Peripheral")
                .CreateManaged(RxApp.MainThreadScheduler);
        }


        public override void OnDisappearing() => this.Peripheral.Dispose();


        public ICommand ToggleRssi { get; }
        public ManagedPeripheral Peripheral { get; private set; }
        [Reactive] public bool IsRssi { get; private set; }
    }
}
