using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;


namespace Shiny.BluetoothLE.Managed
{
    public class ManagedScan
    {
        readonly IBleManager bleManager;
        IDisposable? scanSub;


        public ManagedScan(IBleManager bleManager, IScheduler? scheduler = null)
        {
            this.bleManager = bleManager;
            this.scheduler = scheduler;
        }


        public ObservableCollection<ManagedScanResult> Peripherals { get; } = new ObservableCollection<ManagedScanResult>();
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


        public bool Toggle()
        {
            if (this.IsScanning)
                this.Stop();
            else
                this.Start();

            return this.IsScanning;
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
                        result = new ManagedScanResult(scanResult.Peripheral, scanResult.AdvertisementData?.ServiceUuids);
                        this.Peripherals.Add(result);
                    }
                    result.Connectable = scanResult.AdvertisementData?.IsConnectable;
                    result.ManufacturerData = scanResult.AdvertisementData?.ManufacturerData;
                    result.Name = scanResult.Peripheral.Name;
                    result.Rssi = scanResult.Rssi;
                    result.LastSeen = DateTimeOffset.UtcNow;
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
}
