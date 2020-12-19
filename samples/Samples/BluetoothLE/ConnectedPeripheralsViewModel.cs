using System;
using System.Linq;
using System.Reactive.Linq;
using System.Windows.Input;
using ReactiveUI;
using Shiny.BluetoothLE;


namespace Samples.BluetoothLE
{
    public class ConnectedPeripheralsViewModel : ViewModel
    {
        public ConnectedPeripheralsViewModel(IBleManager centralManager)
        {
            this.Load = ReactiveCommand.CreateFromTask(async () =>
            {
                var peripherals = await centralManager.GetConnectedPeripherals();
                //peripherals.Select(x => x
                // TODO: get TBD connected devices too
            });
        }


        public ICommand Load { get;  }


        public override void OnAppearing()
        {
            base.OnAppearing();
            this.Load.Execute(null);
        }
    }
}

