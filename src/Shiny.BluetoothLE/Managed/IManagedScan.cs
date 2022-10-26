using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reactive.Concurrency;
using System.Threading.Tasks;


namespace Shiny.BluetoothLE.Managed
{
    public interface IManagedScan : IDisposable
    {
        ObservableCollection<ManagedScanResult> Peripherals { get; }
        TimeSpan? ClearTime { get; }
        TimeSpan BufferTimeSpan { get; }
        bool IsScanning { get; }
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
}