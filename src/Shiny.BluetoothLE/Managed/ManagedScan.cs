using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;


namespace Shiny.BluetoothLE.Managed
{
    public class ManagedScan : IDisposable
    {
        readonly IBleManager bleManager;
        IDisposable? scanSub;
        IDisposable? clearSub;


        public ManagedScan(IBleManager bleManager, IScheduler? scheduler = null, TimeSpan? clearTime)
        {
            this.bleManager = bleManager;
            this.scheduler = scheduler;
            this.ClearTime = clearTime;
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


        TimeSpan? clearTime;
        public TimeSpan? ClearTime
        {
            get => this.clearTime;
            set
            {
                this.clearTime = value;
                this.clearSub?.Dispose();

                if (value != null)
                {
                    this.clearSub = Observable
                        .Interval(TimeSpan.FromSeconds(10))
                        .ObserveOnIf(this.Scheduler)
                        .Synchronize(this.Peripherals)
                        .Subscribe(_ =>
                        {
                            var maxAge = DateTimeOffset.UtcNow.Subtract(value.Value);
                            var tmp = this.Peripherals.Where(x => x.LastSeen < maxAge).ToList();

                            foreach (var p in tmp)
                                this.Peripherals.Remove(p);
                        });
                }
            }
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
                .Buffer(TimeSpan.FromSeconds(3))
                .ObserveOnIf(this.Scheduler)
                .Synchronize(this.Peripherals)
                .Subscribe(scanResults =>
                {
                    foreach (var scanResult in scanResults)
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
                    }
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


        public void Dispose()
        {
            this.Stop();
            this.clearSub?.Dispose();
            this.Peripherals.Clear();
        }
    }
}
