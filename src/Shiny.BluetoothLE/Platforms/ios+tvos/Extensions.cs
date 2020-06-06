using System;
using System.Reactive;
using System.Reactive.Linq;
using CoreBluetooth;


namespace Shiny.BluetoothLE
{
    internal static class iOSExtensions
    {
        public static IObservable<Unit> WhenReady(this CBCentralManager manager) => Observable.Create<Unit>(ob =>
        {
            var context = manager.Delegate as CentralContext;
            if (context == null)
                throw new ArgumentException("CBCentralManager.Delegate is not CentralContext");

            return context
                .StateUpdated
                .StartWith(manager.State.FromNative())
                .Subscribe(state =>
                {
                    switch (state)
                    {
                        case AccessState.Available:
                            ob.Respond(Unit.Default);
                            break;

                        case AccessState.Unknown:
                            // not there yet, chill for a second
                            break;

                        default:
                            ob.OnError(new InvalidOperationException("Invalid Adapter State - " + state));
                            break;
                    }
                });
        });


        public static AccessState FromNative(this CBCentralManagerState state)
        {
            switch (state)
            {
                case CBCentralManagerState.PoweredOff:
                    return AccessState.Disabled;

                case CBCentralManagerState.Resetting:
                case CBCentralManagerState.PoweredOn:
                    return AccessState.Available;

                case CBCentralManagerState.Unauthorized:
                    return AccessState.Denied;

                case CBCentralManagerState.Unsupported:
                    return AccessState.NotSupported;

                case CBCentralManagerState.Unknown:
                default:
                    return AccessState.Unknown;
            }
        }


        public static bool IsEqual(this CBPeripheral peripheral, CBPeripheral other)
        {
            if (Object.ReferenceEquals(peripheral, other))
                return true;

            if (peripheral.UUID.Equals(other.UUID))
                return true;

            return false;
        }
    }
}
