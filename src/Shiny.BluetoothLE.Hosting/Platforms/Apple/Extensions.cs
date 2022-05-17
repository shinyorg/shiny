using System;
using System.Reactive;
using System.Reactive.Linq;
using CoreBluetooth;


namespace Shiny.BluetoothLE
{
    public static class BleExtensions
    {
        public static IObservable<Unit> WhenReady(this CBPeripheralManager manager) => Observable.Create<Unit>(ob =>
        {
            var handler = new EventHandler((sender, args) =>
            {
                if (manager.State == CBPeripheralManagerState.PoweredOn)
                    ob.Respond(Unit.Default);
                else
                    ob.OnError(new ArgumentException("Adapter state is invalid - " + manager.State));
            });
            switch (manager.State)
            {
                case CBPeripheralManagerState.Unknown:
                    manager.StateUpdated += handler;
                    break;

                case CBPeripheralManagerState.PoweredOn:
                    ob.Respond(Unit.Default);
                    break;

                default:
                    ob.OnError(new ArgumentException("Adapter state is invalid - " + manager.State));
                    break;
            }
            return () => manager.StateUpdated -= handler;
        });
    }
}