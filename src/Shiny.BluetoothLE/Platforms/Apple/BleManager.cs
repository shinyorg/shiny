using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Microsoft.Extensions.Logging;
using CoreBluetooth;
using Foundation;

namespace Shiny.BluetoothLE;


public class BleManager : CBCentralManagerDelegate, IBleManager
{
    readonly IServiceProvider services;
    readonly ILogger logger;
    readonly ILogger<IPeripheral> peripheralLogger;
    readonly AppleBleConfiguration config;

    public BleManager(
        IServiceProvider services,
        ILogger<IBleManager> logger,
        ILogger<IPeripheral> peripheralLogger,
        AppleBleConfiguration config
    )
    {
        this.services = services;
        this.logger = logger;
        this.peripheralLogger = peripheralLogger;
        this.config = config;
    }

    public bool IsScanning { get; private set; }


    CBCentralManager? manager;
    public CBCentralManager Manager
    {
        get
        {
            if (this.manager == null)
            {
                if (!AppleExtensions.HasPlistValue("NSBluetoothPeripheralUsageDescription"))
                    this.logger.LogCritical("NSBluetoothPeripheralUsageDescription needs to be set - you will likely experience a native crash after this log");

                if (!AppleExtensions.HasPlistValue("NSBluetoothAlwaysUsageDescription", 13))
                    this.logger.LogCritical("NSBluetoothAlwaysUsageDescription needs to be set - you will likely experience a native crash after this log");

                var background = this.services.GetService(typeof(IBleDelegate)) != null;
                if (!background)
                    return new CBCentralManager(this, null);

                var opts = new CBCentralInitOptions
                {
                    ShowPowerAlert = this.config.ShowPowerAlert,
                    RestoreIdentifier = this.config.RestoreIdentifier ?? "shinyble"
                };

                this.manager = new CBCentralManager(this, null, opts);
                this.manager.Delegate = this;
            }
            return this.manager;
        }
    }


    public IObservable<AccessState> RequestAccess() => Observable.Create<AccessState>(ob =>
    {
        IDisposable? disp = null;
        if (this.Manager.State.IsUnknown())
        {
            disp = this.stateUpdatedSubj.Subscribe(x => ob.Respond(x));
        }
        else
        {
            ob.Respond(this.Manager.State.FromNative());
        }
        return () => disp?.Dispose();
    });


    public IPeripheral? GetKnownPeripheral(string peripheralUuid)
    {
        //var uuid = new NSUuid(peripheralUuid);
        //var peripheral = this.Manager
        //    .RetrievePeripheralsWithIdentifiers(uuid)
        //    .FirstOrDefault();

        //if (peripheral == null)
        //    return Observable.Return<IPeripheral?>(null);

        //var device = this.GetPeripheral(peripheral);
        //return Observable.Return(device);
        return this.peripherals.Values.FirstOrDefault(x => x.Uuid.Equals(peripheralUuid, StringComparison.InvariantCultureIgnoreCase));
    }


    public IEnumerable<IPeripheral> GetConnectedPeripherals()
        => this.peripherals.Where(x => x.Value.Status == ConnectionState.Connected).Select(x => x.Value);
    


    static readonly PeripheralScanningOptions peripheralScanningOptions = new PeripheralScanningOptions { AllowDuplicatesKey = true };

    public IObservable<ScanResult> Scan(ScanConfig? config = null) => Observable.Create<ScanResult>(ob =>
    {
        if (this.IsScanning)
            throw new ArgumentException("There is already an existing scan");

        this.Clear();
        config ??= new ScanConfig();
        var sub = this.RequestAccess()
            .Do(access =>
            {
                if (access != AccessState.Available)
                    throw new PermissionException("BluetoothLE", access);
            })
            .SelectMany(_ =>
            {
                if (config.ServiceUuids == null || config.ServiceUuids.Length == 0)
                {
                    this.Manager.ScanForPeripherals(
                        null!,
                        peripheralScanningOptions
                    );
                }
                else
                {
                    var uuids = config.ServiceUuids.Select(CBUUID.FromString).ToArray();
                    this.Manager.ScanForPeripherals(uuids, peripheralScanningOptions);
                }
                return this.ScanResultReceived;
            })
            .Subscribe(
                x => ob.OnNext(x),
                ex => ob.OnError(ex),
                () => ob.OnCompleted()
            );

        return () =>
        {
            this.Manager.StopScan();
            sub?.Dispose();
        };
    });


    public void StopScan() => this.Manager.StopScan();
    

    public override async void WillRestoreState(CBCentralManager central, NSDictionary dict)
    {
        //this.Manager = central;
        var peripheralArray = (NSArray)dict[CBCentralManager.RestoredStatePeripheralsKey];
        if (peripheralArray == null)
            return;

        for (nuint i = 0; i < peripheralArray.Count; i++)
        {
            var item = peripheralArray.GetItem<CBPeripheral>(i);
            var peripheral = this.GetPeripheral(item);
            await this.services
                .RunDelegates<IBleDelegate>(
                    x => x.OnPeripheralStateChanged(peripheral),
                    this.logger
                )
                .ConfigureAwait(false);
        }
        // TODO: restore scan? CBCentralManager.RestoredStateScanOptionsKey
    }


    readonly Subject<(bool Connected, CBPeripheral Peripheral)> connectedSubj = new();
    public override void ConnectedPeripheral(CBCentralManager central, CBPeripheral peripheral)
        => this.RunStateChange(peripheral, true);


    public override void DisconnectedPeripheral(CBCentralManager central, CBPeripheral peripheral, NSError? error)
        => this.RunStateChange(peripheral, false);


    public Subject<ScanResult> ScanResultReceived { get; } = new();
    public override void DiscoveredPeripheral(CBCentralManager central, CBPeripheral peripheral, NSDictionary advertisementData, NSNumber rssi)
    {
        var result = new ScanResult(
            this.GetPeripheral(peripheral),
            rssi?.Int32Value ?? 0,
            new AdvertisementData(advertisementData)
        );
        this.ScanResultReceived.OnNext(result);
    }


    //public Subject<PeripheralConnectionFailed> FailedConnection { get; } = new();
    //public override void FailedToConnectPeripheral(CBCentralManager central, CBPeripheral peripheral, NSError? error)
    //    => this.FailedConnection.OnNext(new PeripheralConnectionFailed(peripheral, error));


    readonly Subject<AccessState> stateUpdatedSubj = new();
    public override void UpdatedState(CBCentralManager central)
    {
        var state = central.State.FromNative();
        if (state == AccessState.Unknown)
            return;

        this.stateUpdatedSubj.OnNext(state);
        this.services.RunDelegates<IBleDelegate>(
            x => x.OnAdapterStateChanged(state),
            this.logger
        );
    }


    void Clear() => this.peripherals
        .Where(x => x.Value.Status != ConnectionState.Connected)
        .ToList()
        .ForEach(x => this.peripherals.TryRemove(x.Key, out var device));


    void RunStateChange(CBPeripheral peripheral, bool connected)
    {
        var p = this.GetPeripheral(peripheral);

        // TODO: conn failures
        var status = connected ? ConnectionState.Connected : ConnectionState.Disconnected;
        p.ConnectionSubject.OnNext(status);

        this.services.RunDelegates<IBleDelegate>(x => x.OnPeripheralStateChanged(p), this.logger);
        this.connectedSubj.OnNext((connected, peripheral));
    }


    readonly ConcurrentDictionary<string, Peripheral> peripherals = new();
    Peripheral GetPeripheral(CBPeripheral peripheral) => this.peripherals.GetOrAdd(
        peripheral.Identifier.ToString(),
        x => new Peripheral(this, peripheral, this.peripheralLogger)
    );
}