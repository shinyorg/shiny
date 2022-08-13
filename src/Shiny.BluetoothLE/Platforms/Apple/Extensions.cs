using System;
using System.Reactive;
using System.Reactive.Linq;
using CoreBluetooth;
using Foundation;
using Shiny.BluetoothLE.Internals;


namespace Shiny.BluetoothLE
{
    internal static class PlatformExtensions
    {
        public static byte[]? ToByteArray(this CBDescriptor native) => (native.Value as NSData)?.ToArray();


        public static IObservable<Unit> WhenReady(this CBCentralManager manager) => Observable.Create<Unit>(ob =>
        {
            var context = manager.Delegate as ManagerContext;
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


#if XAMARIN
        public static bool IsUnknown(this CBCentralManagerState state)
            => state == CBCentralManagerState.Unknown;


        public static AccessState FromNative(this CBCentralManagerState state) => state switch
        {
            CBCentralManagerState.Resetting => AccessState.Available,
            CBCentralManagerState.PoweredOn => AccessState.Available,
            CBCentralManagerState.PoweredOff => AccessState.Disabled,
            CBCentralManagerState.Unauthorized => AccessState.Denied,
            CBCentralManagerState.Unsupported => AccessState.NotSupported,
            _ => AccessState.Unknown
        };
#else
        public static bool IsUnknown(this CBManagerState state)
            => state == CBManagerState.Unknown;


        public static AccessState FromNative(this CBManagerState state) => state switch
        {
            CBManagerState.Resetting => AccessState.Available,
            CBManagerState.PoweredOn => AccessState.Available,
            CBManagerState.PoweredOff => AccessState.Disabled,
            CBManagerState.Unauthorized => AccessState.Denied,
            CBManagerState.Unsupported => AccessState.NotSupported,
            _ => AccessState.Unknown
        };
#endif


        public static bool IsEqual(this CBPeripheral peripheral, CBPeripheral other)
        {
            if (Object.ReferenceEquals(peripheral, other))
                return true;

            if (peripheral.Identifier.Equals(other.Identifier))
                return true;

            return false;
        }
    }
}
