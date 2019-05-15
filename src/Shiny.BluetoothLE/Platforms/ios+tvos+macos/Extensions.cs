using System;
using System.Reactive;
using System.Reactive.Linq;
using Foundation;
using CoreBluetooth;
using Shiny.BluetoothLE.Central;

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


        public static Guid ToGuid(this CBUUID uuid)
        {
            var id = uuid.ToString();
            if (id.Length == 4)
                id = $"0000{id}-0000-1000-8000-00805f9b34fb";

            return Guid.ParseExact(id, "d");
        }


        public static CBUUID ToCBUuid(this Guid guid) => CBUUID.FromString(guid.ToString());
    }
}