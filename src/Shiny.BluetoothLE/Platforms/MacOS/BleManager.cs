using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Microsoft.Extensions.Logging;
using CoreBluetooth;
using Foundation;
using Shiny.BluetoothLE.Intrastructure;

namespace Shiny.BluetoothLE;


public class BleManager : CBCentralManagerDelegate, IBleManager
{
    readonly IServiceProvider services;
    readonly IOperationQueue operations;
    readonly ILogger logger;
    readonly ILogger<Peripheral> peripheralLogger;
    readonly AppleBleConfiguration config;

    public BleManager(
        AppleBleConfiguration config,
        IServiceProvider services,
        IOperationQueue operations,
        ILogger<BleManager> logger,
        ILogger<Peripheral> peripheralLogger
    )
    {
        this.config = config;
        this.operations = operations;
        this.services = services;
        this.logger = logger;
        this.peripheralLogger = peripheralLogger;
    }

    public bool IsScanning { get; private set; }


    CBCentralManager? manager;
    public CBCentralManager Manager
    {
        get
        {
            if (this.manager == null)
            {
                var opts = new CBCentralInitOptions
                {
                    ShowPowerAlert = this.config.ShowPowerAlert
                };

                this.manager = new CBCentralManager(this, this.config.DispatchQueue, opts);
                this.manager.Delegate = this;
            }
            return this.manager;
        }
    }


    /// <summary>
    /// Whether the central is actually powered on. Connect calls below this are silent no-ops, so
    /// the peripheral parks them instead of issuing them (issue #1652).
    /// </summary>
    internal bool IsAdapterAvailable => this.Manager.State == CBManagerState.PoweredOn;


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
        => this.peripherals.Values.FirstOrDefault(x => x.Uuid.Equals(peripheralUuid, StringComparison.InvariantCultureIgnoreCase));


    public IEnumerable<IPeripheral> GetConnectedPeripherals()
        => this.peripherals.Where(x => x.Value.Status == ConnectionState.Connected).Select(x => x.Value);


    static readonly PeripheralScanningOptions peripheralScanningOptions = new PeripheralScanningOptions { AllowDuplicatesKey = true };

    // Guards the scan state below - Scan/StopScan arrive on the caller's thread while the adapter
    // hook runs on the manager's dispatch queue.
    readonly object scanLock = new();

    // Set while a scan subscription is alive, whether or not the native scan is actually running.
    // ScanForPeripherals below PoweredOn is an API-misuse no-op that CoreBluetooth never reports
    // on, and a freshly built CBCentralManager reports Unknown until UpdatedState lands - so the
    // request is parked here and the adapter hook replays it on power-on (issue #1653).
    bool scanRequested;
    CBUUID[]? scanServiceUuids;

    public IObservable<ScanResult> Scan(ScanConfig? scanConfig = null) => Observable.Create<ScanResult>(ob =>
    {
        scanConfig ??= new ScanConfig();

        // Resolved before anything is mutated so a malformed uuid throws straight out of Subscribe
        // without leaving the scan slot claimed.
        var uuids = scanConfig.ServiceUuids is { Length: > 0 }
            ? scanConfig.ServiceUuids.Select(CBUUID.FromString).ToArray()
            : null;

        lock (this.scanLock)
        {
            // Keyed off the request rather than IsScanning: a parked scan is not running yet, but
            // it still owns the single scan slot.
            if (this.scanRequested)
                throw new InvalidOperationException("There is already an existing scan");

            // Both set together - the adapter hook can fire between here and the start below, and
            // it would otherwise issue the parked scan with no service filter.
            this.scanRequested = true;
            this.scanServiceUuids = uuids;
        }
        this.Clear();

        // Subscribed ahead of the native call on purpose - CoreBluetooth can deliver a cached
        // advertisement the instant the scan starts, and subscribing afterwards dropped it.
        var sub = this.ScanResultReceived
            .Subscribe(
                ob.OnNext,
                ob.OnError,
                ob.OnCompleted
            );

        lock (this.scanLock)
            this.StartNativeScan();

        return () =>
        {
            this.StopScan();
            sub.Dispose();
        };
    });


    /// <summary>
    /// Issues the requested scan against CoreBluetooth, but only once the central is actually
    /// powered on. Called again from the adapter hook so a scan asked for against a cold or
    /// powered-off central starts as soon as the adapter comes up (issue #1653).
    /// </summary>
    void StartNativeScan()
    {
        if (!this.scanRequested || this.IsScanning)
            return;

        // Touches Manager on purpose even when it is not up yet - building it is what gets
        // CoreBluetooth to deliver the UpdatedState that brings us back here.
        if (!this.IsAdapterAvailable)
        {
            this.logger.ScanDeferred();
            return;
        }

        this.Manager.ScanForPeripherals(this.scanServiceUuids!, peripheralScanningOptions);
        this.IsScanning = true;
        this.logger.ScanStarted(this.scanServiceUuids?.Length ?? 0);
    }


    public void StopScan()
    {
        lock (this.scanLock)
        {
            this.scanRequested = false;
            this.scanServiceUuids = null;
            this.IsScanning = false;
        }
        this.Manager.StopScan();
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
        this.logger.ManagerStateChange(central.State);

        var state = central.State.FromNative();
        if (state == AccessState.Unknown)
            return;

        this.stateUpdatedSubj.OnNext(state);

        // Keyed off the native state rather than the mapped AccessState: Resetting maps to
        // Available, but CoreBluetooth invalidates every connection for it just as it does for
        // PoweredOff. Ahead of the delegates on purpose - a consumer handling OnAdapterStateChanged
        // should see peripheral state that already agrees with the adapter (issue #1652).
        this.OnAdapterStateChanged(central.State == CBManagerState.PoweredOn);

        await this.services.RunDelegates<IBleDelegate>(
            x => x.OnAdapterStateChanged(state),
            this.logger
        );
    }


    void OnAdapterStateChanged(bool available)
    {
        lock (this.scanLock)
        {
            if (available)
            {
                this.StartNativeScan();
            }
            else
            {
                // CoreBluetooth tears the scan down when the adapter drops. The request stays
                // parked so it resumes on power-on instead of dying silently (issue #1653).
                this.IsScanning = false;
            }
        }

        foreach (var peripheral in this.peripherals.Values)
        {
            try
            {
                if (available)
                    peripheral.OnAdapterAvailable();
                else
                    peripheral.OnAdapterUnavailable();
            }
            catch (Exception ex)
            {
                this.logger.LogWarning(ex, "Error applying adapter state to peripheral {Uuid}", peripheral.Uuid);
            }
        }
    }


    void Clear() => this.peripherals
        .Where(x => x.Value.Status != ConnectionState.Connected && !x.Value.IsAwaitingReconnect)
        .ToList()
        .ForEach(x => this.peripherals.TryRemove(x.Key, out var device));


    void RunStateChange(CBPeripheral peripheral, bool connected, NSError? error)
    {
        this.logger.PeripheralStateChange(peripheral.Identifier, connected, error?.LocalizedDescription ?? "None");

        var p = this.GetPeripheral(peripheral);
        var status = connected ? ConnectionState.Connected : ConnectionState.Disconnected;
        p.ReceiveStateChange(status);

        this.services.RunDelegates<IBleDelegate>(x => x.OnPeripheralStateChanged(p), this.logger);
    }


    readonly ConcurrentDictionary<string, Peripheral> peripherals = new();
    Peripheral GetPeripheral(CBPeripheral peripheral) => this.peripherals.GetOrAdd(
        peripheral.Identifier.ToString(),
        x => new Peripheral(this, peripheral, this.operations, this.peripheralLogger)
    );
}
