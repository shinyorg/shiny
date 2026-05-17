using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;

namespace Shiny.BluetoothLE;


/// <summary>
/// Pairing state between the central and a peripheral.
/// </summary>
public enum PairingState
{
    /// <summary>The peripheral is not paired with this device.</summary>
    NotPaired,

    /// <summary>The peripheral is paired (bonded) with this device.</summary>
    Paired
}

/// <summary>
/// Optional manager capability for enumerating peripherals already paired with the OS.
/// </summary>
public interface ICanViewPairedPeripherals : IBleManager
{
    /// <summary>
    /// Gets the peripherals currently paired (bonded) with the operating system.
    /// </summary>
    /// <returns>The paired peripherals.</returns>
    IReadOnlyList<IPeripheral> GetPairedPeripherals();
}


/// <summary>
/// Optional peripheral capability for initiating pairing requests from the app.
/// </summary>
public interface ICanPairPeripherals : IPeripheral
{
    /// <summary>
    /// Initiates a pairing/bonding request with the peripheral.
    /// </summary>
    /// <param name="pin">Optional PIN/passkey used during pairing on platforms that support it.</param>
    /// <returns>A single-value observable that emits true on successful pairing, false on failure.</returns>
    IObservable<bool> PairingRequest(string? pin = null);

    /// <summary>
    /// Gets the current pairing state for this peripheral.
    /// </summary>
    PairingState PairingStatus { get; }
}


/// <summary>
/// Helpers for working with optional pairing capabilities.
/// </summary>
public static class FeaturePairing
{
    /// <summary>
    /// Returns true when the BLE manager supports enumerating paired peripherals on the current platform.
    /// </summary>
    /// <param name="centralManager">The BLE manager to test.</param>
    /// <returns>True when paired peripherals can be listed; otherwise false.</returns>
    public static bool CanViewPairedPeripherals(this IBleManager centralManager) => centralManager is ICanViewPairedPeripherals;

    /// <summary>
    /// Returns true when the peripheral supports app-initiated pairing requests on the current platform.
    /// </summary>
    /// <param name="peripheral">The peripheral to test.</param>
    /// <returns>True when pairing requests are supported; otherwise false.</returns>
    public static bool IsPairingRequestsAvailable(this IPeripheral peripheral) => peripheral is ICanPairPeripherals;


    /// <summary>
    /// Returns paired peripherals if supported by the platform, otherwise an empty list.
    /// </summary>
    /// <param name="centralManager">The BLE manager.</param>
    /// <returns>The paired peripherals or an empty list.</returns>
    public static IReadOnlyList<IPeripheral> TryGetPairedPeripherals(this IBleManager centralManager)
    {
        var paired = centralManager as ICanViewPairedPeripherals;
        if (paired == null)
            return Array.Empty<IPeripheral>().ToList();

        return paired.GetPairedPeripherals();
    }


    /// <summary>
    /// Returns the peripheral's pairing state if supported, otherwise null.
    /// </summary>
    /// <param name="peripheral">The peripheral to query.</param>
    /// <returns>The pairing state, or null if pairing is not exposed on this platform.</returns>
    public static PairingState? TryGetPairingStatus(this IPeripheral peripheral)
    {
        if (peripheral is ICanPairPeripherals pair)
            return pair.PairingStatus;

        return null;
    }


    /// <summary>
    /// Initiates a pairing request if supported, otherwise returns an observable that yields null.
    /// </summary>
    /// <param name="peripheral">The peripheral to pair with.</param>
    /// <param name="pin">Optional PIN/passkey.</param>
    /// <returns>A single-value observable with the pairing outcome, or null when unsupported.</returns>
    public static IObservable<bool?> TryPairingRequest(this IPeripheral peripheral, string? pin = null)
    {
        if (peripheral is ICanPairPeripherals pair)
            return pair.PairingRequest(pin).Select(x => (bool?)x);

        return Observable.Return<bool?>(null);
    }
}
