using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;

namespace Shiny.BluetoothLE.Managed;


public class ManagedScan : IDisposable, IManagedScan
{
    readonly Subject<(ManagedScanListAction Action, ManagedScanResult? ScanResult)> actionSubj = new();
    readonly ObservableList<ManagedScanResult> list = new();
    readonly object syncLock = new();
    readonly IBleManager bleManager;
    CompositeDisposable? disposer;


    public ManagedScan(IBleManager bleManager)
        => this.bleManager = bleManager;


    public IEnumerable<IPeripheral> GetConnectedPeripherals() => this.list
        .ToList()
        .Where(x => x.Peripheral.Status == ConnectionState.Connected)
        .Select(x => x.Peripheral);


    public IObservable<(ManagedScanListAction Action, ManagedScanResult? ScanResult)> WhenScan() => this.actionSubj;

    public INotifyReadOnlyCollection<ManagedScanResult> Peripherals => this.list;
    public bool IsScanning { get; private set; }
    public TimeSpan BufferTimeSpan { get; private set; }
    public ScanConfig? ScanConfig { get; private set; }
    public IScheduler? Scheduler { get; private set; }
    public TimeSpan? ClearTime { get; private set; }


    public async Task Start(
        ScanConfig? scanConfig = null,
        Func<ScanResult, bool>? predicate = null,
        IScheduler? scheduler = null,
        TimeSpan? bufferTime = null,
        TimeSpan? clearTime = null
    )
    {
        if (this.IsScanning)
            throw new InvalidOperationException("A scan is already running");

        this.ScanConfig = scanConfig;
        this.Scheduler = scheduler;
        this.BufferTimeSpan = bufferTime ?? TimeSpan.FromSeconds(3);
        this.ClearTime = clearTime;

        var access = await this.bleManager
            .RequestAccess()
            .ToTask()
            .ConfigureAwait(false);

        access.Assert();
        this.list.Clear();
        this.actionSubj.OnNext((ManagedScanListAction.Clear, null));
        this.disposer = new();

        this.bleManager
            .Scan(this.ScanConfig)
            .Buffer(this.BufferTimeSpan)
            .Where(x => x?.Any() ?? false)
            .ObserveOnIf(this.Scheduler)
            .Subscribe(
                scanResults =>
                {
                    foreach (var scanResult in scanResults)
                    {
                        var show = predicate?.Invoke(scanResult!) ?? true;
                        if (show)
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
                                lock (this.syncLock)
                                    this.list.Add(result);
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
                    }
                },
                this.actionSubj.OnError
            )
            .DisposedBy(this.disposer);

        this.IsScanning = true;


        if (clearTime != null)
        {
            Observable
                .Interval(TimeSpan.FromSeconds(10))
                .ObserveOnIf(this.Scheduler)
                .Subscribe(
                    _ =>
                    {
                        var maxAge = DateTimeOffset.UtcNow.Subtract(clearTime.Value);
                        var tmp = this.Peripherals.Where(x => x.LastSeen < maxAge).ToList();

                        foreach (var p in tmp)
                        {
                            this.actionSubj.OnNext((ManagedScanListAction.Remove, p));
                            lock (this.syncLock)
                                this.list.Remove(p);
                        }
                    },
                    ex =>
                    {
                        this.actionSubj.OnError(ex);
                        this.Stop();
                    }
                )
                .DisposedBy(this.disposer);
        }
    }


    public void Dispose()
    {
        this.Stop();
        this.list.Clear();
    }


    public void Stop()
    {
        this.disposer?.Dispose();
        this.IsScanning = false;
    }
}
