using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;


namespace Shiny.BluetoothLE.Managed
{
    public class ManagedScan : IDisposable, IManagedScan
    {
        readonly Subject<(ManagedScanListAction Action, ManagedScanResult? ScanResult)> actionSubj;
        readonly IBleManager bleManager;
        IDisposable? scanSub;
        IDisposable? clearSub;


        public ManagedScan(IBleManager bleManager,
                           ScanConfig? scanConfig = null,
                           IScheduler? scheduler = null,
                           TimeSpan? clearTime = null)
        {
            this.actionSubj = new Subject<(ManagedScanListAction Action, ManagedScanResult? ScanResult)>();
            this.bleManager = bleManager;

            this.scanConfig = scanConfig;
            this.scheduler = scheduler;
            this.clearTime = clearTime;
        }


        public IEnumerable<IPeripheral> GetConnectedPeripherals() => this.Peripherals
            .ToList()
            .Where(x => x.Peripheral.IsConnected())
            .Select(x => x.Peripheral);


        public IObservable<(ManagedScanListAction Action, ManagedScanResult? ScanResult)> WhenScan() => this.actionSubj;
        public ObservableCollection<ManagedScanResult> Peripherals { get; } = new ObservableCollection<ManagedScanResult>();
        public bool IsScanning => this.scanSub != null;


        ScanConfig? scanConfig;
        public ScanConfig? ScanConfig
        {
            get => this.scanConfig;
            set => this.Flip(() => this.scanConfig = value);
        }


        public TimeSpan BufferTimeSpan { get; set; } = TimeSpan.FromSeconds(3);

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
                            {
                                this.Peripherals.Remove(p);
                                this.actionSubj.OnNext((ManagedScanListAction.Remove, p));
                            }
                        });
                }
            }
        }


        public async Task<bool> Toggle()
        {
            if (this.IsScanning)
                this.Stop();
            else
                await this.Start();

            return this.IsScanning;
        }


        public async Task Start()
        {
            if (this.IsScanning)
                return;

            // restart clear timer if set
            this.ClearTime = this.ClearTime;
            if (this.Peripherals.Count > 0)
            {
                this.Peripherals.Clear();
                this.actionSubj.OnNext((ManagedScanListAction.Clear, null));
            }

            var tcs = new TaskCompletionSource<object>();
            this.scanSub = this.bleManager
                .Scan(this.ScanConfig)
                .DoOnce(x => tcs.TrySetResult(null))
                .Buffer(this.BufferTimeSpan)
                .Where(x => x?.Any() ?? false)
                .ObserveOnIf(this.Scheduler)
                .Synchronize(this.Peripherals)
                .Subscribe(
                    scanResults =>
                    {
                        foreach (var scanResult in scanResults)
                        {
                            var action = ManagedScanListAction.Update;
                            var result = this.Peripherals.FirstOrDefault(x => x.Peripheral.Equals(scanResult.Peripheral));
                            if (result == null)
                            {
                                action = ManagedScanListAction.Add;
                                result = new ManagedScanResult(scanResult.Peripheral, scanResult.AdvertisementData?.ServiceUuids);
                                this.Peripherals.Add(result);
                            }
                            result.Connectable = scanResult.AdvertisementData?.IsConnectable;
                            result.ManufacturerData = scanResult.AdvertisementData?.ManufacturerData;
                            result.Name = scanResult.Peripheral.Name;
                            result.Rssi = scanResult.Rssi;
                            result.LastSeen = DateTimeOffset.UtcNow;
                            this.actionSubj.OnNext((action, result));
                        }
                    },
                    ex => tcs.TrySetException(ex)
                );

            await tcs.Task.ConfigureAwait(false);
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
