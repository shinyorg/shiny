using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;

namespace Shiny.BluetoothLE.Managed;


public class ManagedScan : IDisposable, IManagedScan
{
    readonly Subject<(ManagedScanListAction Action, ManagedScanResult? ScanResult)> actionSubj = new();
    readonly IBleManager bleManager;
    IDisposable? scanSub;
    IDisposable? clearSub;


    public ManagedScan(
        IBleManager bleManager,
        ScanConfig? scanConfig = null,
        IScheduler? scheduler = null,
        TimeSpan? clearTime = null
    )
    {
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
    public bool IsScanning { get; private set; }
    public TimeSpan BufferTimeSpan { get; set; } = TimeSpan.FromSeconds(3);


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
                    .Subscribe(
                        _ =>
                        {
                            var maxAge = DateTimeOffset.UtcNow.Subtract(value.Value);
                            var tmp = this.Peripherals.Where(x => x.LastSeen < maxAge).ToList();

                            foreach (var p in tmp)
                            {
                                this.Peripherals.Remove(p);
                                this.actionSubj.OnNext((ManagedScanListAction.Remove, p));
                            }
                        },
                        ex =>
                        {
                            this.actionSubj.OnError(ex);
                            this.Stop();
                        }
                    );
            }
        }
    }


    public async Task Start()
    {
        if (this.IsScanning)
            return;

        var access = await this.bleManager
            .RequestAccess()
            .ToTask()
            .ConfigureAwait(false);

        access.Assert();
        this.Stop();
        this.StartInternal();
    }


    public void Dispose()
    {
        this.Stop();
        this.Peripherals.Clear();
    }


    public void Stop()
    {
        this.IsScanning = false;
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
            this.StartInternal();
    }


    void StartInternal()
    {
        // restart clear timer if set
        this.ClearTime = this.ClearTime;
        if (this.Peripherals.Count > 0)
        {
            this.Peripherals.Clear();
            this.actionSubj.OnNext((ManagedScanListAction.Clear, null));
        }

        this.scanSub = this.bleManager
            .Scan(this.ScanConfig)
            .Do(_ => this.IsScanning = true)
            .Buffer(this.BufferTimeSpan)
            .Where(x => x?.Any() ?? false)
            .ObserveOnIf(this.Scheduler)
            .Finally(() => this.Stop())
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
                            result = new ManagedScanResult(scanResult.Peripheral)
                            {
                                ServiceUuids = scanResult.AdvertisementData?.ServiceUuids,
                                ServiceData = scanResult.AdvertisementData?.ServiceData
                            };
                            this.Peripherals.Add(result);
                        }
                        result.IsConnectable = scanResult.AdvertisementData?.IsConnectable;
                        result.ManufacturerData = scanResult.AdvertisementData?.ManufacturerData;
                        result.Name = scanResult.Peripheral.Name;
                        result.LocalName = scanResult.AdvertisementData?.LocalName;
                        result.Rssi = scanResult.Rssi;
                        result.TxPower = scanResult.AdvertisementData?.TxPower;
                        result.LastSeen = DateTimeOffset.UtcNow;
                        this.actionSubj.OnNext((action, result));
                    }
                },
                this.actionSubj.OnError
            );
    }
}
