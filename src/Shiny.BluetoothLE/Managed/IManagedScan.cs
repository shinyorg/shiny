using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reactive.Concurrency;
using System.Threading.Tasks;


namespace Shiny.BluetoothLE.Managed
{
    public interface IManagedScan : IDisposable
    {
        TimeSpan? ClearTime { get; set; }
        TimeSpan BufferTimeSpan { get; set; }
        bool IsScanning { get; }
        ObservableCollection<ManagedScanResult> Peripherals { get; }
        ScanConfig? ScanConfig { get; set; }
        IScheduler? Scheduler { get; set; }

        IEnumerable<IPeripheral> GetConnectedPeripherals();
        Task Start();
        void Stop();
        Task<bool> Toggle();
        IObservable<(ManagedScanListAction Action, ManagedScanResult? ScanResult)> WhenScan();
    }
}