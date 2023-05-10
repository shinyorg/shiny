using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;

namespace Shiny.BluetoothLE;


public enum PairingState
{
    Unavailiable,
    NotPaired,
    Paired
}

public interface ICanViewPairedPeripherals : IBleManager
{
    /// <summary>
    /// Get the list of paired peripherals
    /// </summary>
    /// <returns></returns>
    IObservable<IEnumerable<IPeripheral>> GetPairedPeripherals();
}


public interface ICanPairPeripherals : IPeripheral
{
    /// <summary>
    /// Send a pairing request
    /// </summary>
    /// <param name="pin"></param>
    /// <returns></returns>
    IObservable<bool> PairingRequest(string? pin = null);
    
    /// <summary>
    /// The current peripheral pairing status
    /// </summary>
    PairingState PairingStatus { get; }
}


public static class FeaturePairing
{
    public static bool CanViewPairedPeripherals(this IBleManager centralManager) => centralManager is ICanViewPairedPeripherals;
    public static bool IsPairingRequestsAvailable(this IPeripheral peripheral) => peripheral is ICanPairPeripherals;


    public static IObservable<IEnumerable<IPeripheral>> TryGetPairedPeripherals(this IBleManager centralManager)
    {
        var paired = centralManager as ICanViewPairedPeripherals;
        if (paired == null)
            return Observable.Empty<IEnumerable<IPeripheral>>();

        return paired.GetPairedPeripherals();
    }


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
