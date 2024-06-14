using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Microsoft.Extensions.Logging;
using CoreBluetooth;
using Foundation;
using Shiny.BluetoothLE.Infrastructure;

namespace Shiny.BluetoothLE;


public class BleManager(
    AppleBleConfiguration config,
    IServiceProvider services,
    IOperationQueue operations,
    ILogger<BleManager> logger,
    ILogger<Peripheral> peripheralLogger
) : CBCentralManagerDelegate, IBleManager
{

    public bool IsScanning { get; private set; }


    CBCentralManager? manager;
    public CBCentralManager Manager
    {
        get
        {
            if (this.manager == null)
            {
                if (!AppleExtensions.HasPlistValue("NSBluetoothPeripheralUsageDescription"))
                    logger.MissingIosPermission("NSBluetoothPeripheralUsageDescription");

                if (!AppleExtensions.HasPlistValue("NSBluetoothAlwaysUsageDescription", 13))
                    logger.MissingIosPermission("NSBluetoothAlwaysUsageDescription");

                var background = services.GetService(typeof(IBleDelegate)) != null;
                if (!background)
                {
                    this.manager = new CBCentralManager(this, config.DispatchQueue);
                    this.manager.Delegate = this;
                }
                else
                {
                    var opts = new CBCentralInitOptions
                    {
                        ShowPowerAlert = config.ShowPowerAlert,
                        RestoreIdentifier = config.RestoreIdentifier ?? "shinyble"
                    };

                    this.manager = new CBCentralManager(this, config.DispatchQueue, opts);
                    this.manager.Delegate = this;
                }
            }
            return this.manager;
        }
    }


    public AccessState CurrentAccess => CBCentralManager.Authorization switch
    {
        CBManagerAuthorization.NotDetermined => AccessState.Unknown,
        CBManagerAuthorization.Restricted => AccessState.Restricted,
        CBManagerAuthorization.Denied => AccessState.Denied,
        CBManagerAuthorization.AllowedAlways => this.Manager.State.FromNative()
    };


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

    public IObservable<ScanResult> Scan(ScanConfig? scanConfig = null) => Observable.Create<ScanResult>(ob =>
    {
        if (this.IsScanning)
            throw new InvalidOperationException("There is already an existing scan");

        this.Clear();
        scanConfig ??= new ScanConfig();
        var sub = this.RequestAccess()
            .Do(access =>
            {
                if (access != AccessState.Available)
                    throw new PermissionException("BluetoothLE", access);
            })
            .SelectMany(_ =>
            {
                if (scanConfig.ServiceUuids == null || scanConfig.ServiceUuids.Length == 0)
                {
                    this.Manager.ScanForPeripherals(
                        null!,
                        peripheralScanningOptions
                    );
                }
                else
                {
                    var uuids = scanConfig.ServiceUuids.Select(CBUUID.FromString).ToArray();
                    this.Manager.ScanForPeripherals(uuids, peripheralScanningOptions);
                }
                this.IsScanning = true;
                return this.ScanResultReceived;
            })
            .Subscribe(
                ob.OnNext,
                ob.OnError, 
                ob.OnCompleted
            );

        return () =>
        {
            this.Manager.StopScan();
            this.IsScanning = false;
            sub?.Dispose();
        };
    });


    public void StopScan()
    {
        this.Manager.StopScan();
        this.IsScanning = false;
    }
    

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
            await services
                .RunDelegates<IBleDelegate>(
                    x => x.OnPeripheralStateChanged(peripheral),
                    logger
                )
                .ConfigureAwait(false);
        }
        // TODO: restore scan? CBCentralManager.RestoredStateScanOptionsKey
    }


    public override void ConnectedPeripheral(CBCentralManager central, CBPeripheral peripheral)
        => this.RunStateChange(peripheral, true, null);


    public override void DisconnectedPeripheral(CBCentralManager central, CBPeripheral peripheral, NSError? error)
        => this.RunStateChange(peripheral, false, error);


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


    public override void FailedToConnectPeripheral(CBCentralManager central, CBPeripheral peripheral, NSError? error)
        => this.GetPeripheral(peripheral).ConnectionFailed(error);


    readonly Subject<AccessState> stateUpdatedSubj = new();
    public override async void UpdatedState(CBCentralManager central)
    {
        logger.ManagerStateChange(central.State);

        var state = central.State.FromNative();
        if (state == AccessState.Unknown)
            return;

        this.stateUpdatedSubj.OnNext(state);
        await services.RunDelegates<IBleDelegate>(
            x => x.OnAdapterStateChanged(state),
            logger
        );
    }


    void Clear() => this.peripherals
        .Where(x => x.Value.Status != ConnectionState.Connected)
        .ToList()
        .ForEach(x => this.peripherals.TryRemove(x.Key, out var device));


    void RunStateChange(CBPeripheral peripheral, bool connected, NSError? error)
    {
        logger.PeripheralStateChange(peripheral.Identifier, connected, error?.LocalizedDescription ?? "None");

        var p = this.GetPeripheral(peripheral);
        var status = connected ? ConnectionState.Connected : ConnectionState.Disconnected;
        p.ReceiveStateChange(status);

        services.RunDelegates<IBleDelegate>(x => x.OnPeripheralStateChanged(p), logger);
    }


    readonly ConcurrentDictionary<string, Peripheral> peripherals = new();
    Peripheral GetPeripheral(CBPeripheral peripheral) => this.peripherals.GetOrAdd(
        peripheral.Identifier.ToString(),
        x => new Peripheral(this, peripheral, operations, peripheralLogger)
    );
}