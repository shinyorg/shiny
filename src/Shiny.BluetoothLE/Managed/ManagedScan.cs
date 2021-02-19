using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;


namespace Shiny.BluetoothLE.Managed
{
    public class ManagedScan : IDisposable
    {
        readonly Subject<(IPeripheral Peripheral, ManagedScanResult ScanResult)> newSubj;
        readonly IBleManager bleManager;
        IDisposable? scanSub;
        IDisposable? clearSub;


        public ManagedScan(IBleManager bleManager,
                           ScanConfig? scanConfig = null,
                           IScheduler? scheduler = null,
                           TimeSpan? clearTime = null)
        {
            this.newSubj = new Subject<(IPeripheral Peripheral, ManagedScanResult ScanResult)>();
            this.bleManager = bleManager;

            this.scanConfig = scanConfig;
            this.scheduler = scheduler;
            this.clearTime = clearTime;
        }


        public IEnumerable<IPeripheral> GetConnectedPeripherals() => this.Peripherals
            .ToList()
            .Where(x => x.Peripheral.IsConnected())
            .Select(x => x.Peripheral);


        public IObservable<(IPeripheral Peripheral, ManagedScanResult ScanResult)> WhenNewPeripheralFound() => this.newSubj;
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

            // restart clear timer if set
            this.ClearTime = this.ClearTime;
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

                            // new device found
                            this.newSubj.OnNext((result.Peripheral, result));
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
            this.clearSub?.Dispose();
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
            this.Peripherals.Clear();
        }
    }
}
