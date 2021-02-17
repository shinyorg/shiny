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


        ScanConfig? scanConfig;
        public ScanConfig? ScanConfig
        {
            get => this.scanConfig;
            set => this.Flip(() => this.scanConfig = value);
        }


        IScheduler? scheduler;
        public IScheduler? Scheduler
        {
            get => this.scheduler;
            set => this.Flip(() => this.scheduler = value);
        }


        public void Toggle()
        {
            if (this.IsScanning)
                this.Stop();
            else
                this.Start();
        }


        public void Start()
        {
            if (this.IsScanning)
                return;

            this.Peripherals.Clear();
            this.scanSub = this.bleManager
                .Scan(this.ScanConfig)
                .ObserveOnIf(this.Scheduler)
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


        void Flip(Action action)
        {
            var wasScanning = this.IsScanning;
            this.Stop();
            action();
            if (wasScanning)
                this.Start();
        }
    }


    public static class Extensions_ManagedScan
    {
        public static ManagedScan CreateManagedScanner(this IBleManager bleManager, bool autoStartScan = false)
            => new ManagedScan(bleManager, autoStartScan);
    }
}
