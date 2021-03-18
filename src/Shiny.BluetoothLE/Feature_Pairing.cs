using System;
using System.Collections.Generic;
using System.Reactive.Linq;


namespace Shiny.BluetoothLE
{
    public interface ICanSeePairedPeripherals : IBleManager
    {
        /// <summary>
        /// Get the list of paired peripherals
        /// </summary>
        /// <returns></returns>
        IObservable<IEnumerable<IPeripheral>> GetPairedPeripherals();
    }


    public interface ICanPairPeripherals
    {
        IObservable<bool> PairingRequest(string? pin = null);
        PairingState PairingStatus { get; }
    }


    public static class FeaturePairing
    {
        public static bool CanViewPairedPeripherals(this IBleManager centralManager) => centralManager is ICanSeePairedPeripherals;
        public static bool IsPairingRequestsAvailable(this IPeripheral peripheral) => peripheral is ICanPairPeripherals;


        public static PairingState? TryGetPairingStatus(this IPeripheral peripheral)
        {
            if (peripheral is ICanPairPeripherals pair)
                return pair.PairingStatus;

            return null;
        }


        public static IObservable<bool?> TryPairingRequest(this IPeripheral peripheral, string? pin = null)
        {
            if (peripheral is ICanPairPeripherals pair)
                return pair.PairingRequest(pin).Select(x => (bool?)x);

            return Observable.Return<bool?>(null);
        }
    }
}
