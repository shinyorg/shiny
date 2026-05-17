using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reactive.Concurrency;
using System.Threading.Tasks;

namespace Shiny.BluetoothLE.Managed;


/// <summary>
/// The kind of change applied to the managed scan's peripheral list.
/// </summary>
public enum ManagedScanListAction
{
    /// <summary>A new peripheral was added to the list.</summary>
    Add,

    /// <summary>An existing peripheral's advertisement was refreshed.</summary>
    Update,

    /// <summary>A peripheral was removed (typically because it has not been seen for the configured clear time).</summary>
    Remove,

    /// <summary>The entire list was cleared (usually at the start of a new scan).</summary>
    Clear
}


/// <summary>
/// Provides a managed BLE scan that maintains an observable collection of discovered peripherals
/// </summary>
public interface IManagedScan : IDisposable
{
    /// <summary>
    /// Gets the time after which peripherals are removed if not re-detected
    /// </summary>
    TimeSpan? ClearTime { get; }

    /// <summary>
    /// Gets the buffer time span for batching scan results
    /// </summary>
    TimeSpan BufferTimeSpan { get; }

    /// <summary>
    /// Gets whether the scan is currently running
    /// </summary>
    bool IsScanning { get; }

    /// <summary>
    /// Gets the observable collection of discovered peripherals
    /// </summary>
    INotifyReadOnlyCollection<ManagedScanResult> Peripherals { get; }

    /// <summary>
    /// Gets the current scan configuration
    /// </summary>
    ScanConfig? ScanConfig { get; }

    /// <summary>
    /// Gets the scheduler used for collection updates
    /// </summary>
    IScheduler? Scheduler { get; }

    /// <summary>
    /// Gets all currently connected peripherals from the scan results
    /// </summary>
    /// <returns>The connected peripherals</returns>
    IEnumerable<IPeripheral> GetConnectedPeripherals();

    /// <summary>
    /// Starts the managed BLE scan
    /// </summary>
    /// <param name="scanConfig">Optional scan configuration</param>
    /// <param name="predicate">Optional filter predicate for scan results</param>
    /// <param name="scheduler">Optional scheduler for collection updates</param>
    /// <param name="bufferTime">Optional buffer time for batching results</param>
    /// <param name="clearTime">Optional time after which stale peripherals are removed</param>
    Task Start(
        ScanConfig? scanConfig = null,
        Func<ScanResult, bool>? predicate = null,
        IScheduler? scheduler = null,
        TimeSpan? bufferTime = null,
        TimeSpan? clearTime = null
    );

    /// <summary>
    /// Stops the managed BLE scan
    /// </summary>
    void Stop();

    /// <summary>
    /// Returns an observable that emits scan list change events
    /// </summary>
    /// <returns>An observable of scan list actions and associated scan results</returns>
    IObservable<(ManagedScanListAction Action, ManagedScanResult? ScanResult)> WhenScan();
}