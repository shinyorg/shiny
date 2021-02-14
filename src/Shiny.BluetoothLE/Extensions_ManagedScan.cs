using System;
using System.Linq;
using System.Collections.ObjectModel;
using System.Reactive.Concurrency;
using System.Reactive.Linq;


namespace Shiny.BluetoothLE
{
    public class ManagedScanPeripheral : NotifyPropertyChanged
    {
        public ManagedScanPeripheral(IPeripheral peripheral) => this.Peripheral = peripheral;

        public IPeripheral Peripheral { get; }


        string name;
        public string Name
        {
            get => this.name;
            internal set => this.Set(ref this.name, value);
        }


        int rssi;
        public int Rssi
        {
            get => this.rssi;
            internal set => this.Set(ref this.rssi, value);
        }

        // TODO: add timestamp?
        // TODO: add service UUIDS?  could just add all of advertisement and keep name/rssi as observable since those can change during scan
    }


    public class ManagedScan
    {
        readonly IBleManager bleManager;
        IDisposable? scanSub;


        public ManagedScan(IBleManager bleManager, bool autoStartScan)
        {
            this.bleManager = bleManager;
            if (autoStartScan)
                this.Start();
        }


        public ObservableCollection<ManagedScanPeripheral> Peripherals { get; } = new ObservableCollection<ManagedScanPeripheral>();
        public bool IsScanning => this.scanSub != null;


        public void Start(ScanConfig? config = null, IScheduler? scheduler = null)
        {
            if (this.IsScanning)
                return;

            this.scanSub = this.bleManager
                .Scan(config)
                .ObserveOnIf(scheduler)
                .Synchronize()
                .Subscribe(scanResult =>
                {
                    var result = this.Peripherals.FirstOrDefault(x => x.Peripheral.Equals(scanResult.Peripheral));
                    if (result == null)
                    {
                        result = new ManagedScanPeripheral(scanResult.Peripheral);
                        this.Peripherals.Add(result);
                    }
                    result.Name = scanResult.Peripheral.Name;
                    result.Rssi = scanResult.Rssi;
                });
        }


        public void Stop()
        {
            this.scanSub?.Dispose();
            this.scanSub = null;
        }
    }


    public static class Extensions_ManagedScan
    {
        public static ManagedScan CreateManagedScanner(this IBleManager bleManager, bool autoStartScan = false)
            => new ManagedScan(bleManager, autoStartScan);
    }
}
