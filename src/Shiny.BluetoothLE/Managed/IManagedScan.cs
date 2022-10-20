using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reactive.Concurrency;
using System.Threading.Tasks;

namespace Shiny.BluetoothLE.Managed;


public enum ManagedScanListAction
{
    Add,
    Update,
    Remove,
    Clear
}


public interface IManagedScan : IDisposable
{
    TimeSpan? ClearTime { get; }
    TimeSpan BufferTimeSpan { get; }
    bool IsScanning { get; }
    INotifyReadOnlyCollection<ManagedScanResult> Peripherals { get; }
    ScanConfig? ScanConfig { get; }
    IScheduler? Scheduler { get; }

    IEnumerable<IPeripheral> GetConnectedPeripherals();
    Task Start(
        ScanConfig? scanConfig = null,
        Func<ScanResult, bool>? predicate = null,
        IScheduler? scheduler = null,
        TimeSpan? bufferTime = null,
        TimeSpan? clearTime = null
    );
    void Stop();
    IObservable<(ManagedScanListAction Action, ManagedScanResult? ScanResult)> WhenScan();
}