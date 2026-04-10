using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Advertisement;
using Windows.Devices.Enumeration;
using Windows.Devices.Radios;
using Windows.Foundation;

namespace Shiny.BluetoothLE;


public partial class BleManager : IBleManager, IShinyStartupTask
{
    static readonly TimeSpan ScanRestartCooldown = TimeSpan.FromMilliseconds(350);
    readonly IServiceProvider services;
    readonly ILogger logger;
    BluetoothLEAdvertisementWatcher? watcher;
    readonly object scanSyncLock = new();
    readonly ConcurrentDictionary<ulong, byte> pendingPeripheralResolves = new();
    int scanGeneration;
    int activeScanGeneration;
    DateTimeOffset lastScanStoppedAtUtc = DateTimeOffset.MinValue;


    public BleManager(IServiceProvider services, ILogger<IBleManager> logger)
    {
        this.services = services;
        this.logger = logger;
    }


    public void Start()
    {
        var delegates = this.services.GetServices<IBleDelegate>().ToList();
        if (delegates.Count == 0)
            return;

        this.GetRadio()
            .Where(x => x != null)
            .Subscribe(
                radio =>
                {
                    var handler = new TypedEventHandler<Radio, object>((sender, args) =>
                    {
                        var status = sender.GetAccessStatus();
                        delegates.RunDelegates(x => x.OnAdapterStateChanged(status), this.logger);
                    });
                    radio!.StateChanged += handler;
                },
                ex =>
                {
                    this.logger.LogError(ex, "Could not monitor radio");
                }
            );
    }


    public AccessState CurrentAccess
    {
        get
        {
            if (this.SelectedRadio == null)
                return AccessState.Unknown;

            return this.SelectedRadio.GetAccessStatus();
        }
    }

    public bool IsScanning { get; private set; }
    public Radio? SelectedRadio { get; set; }


    public IEnumerable<IPeripheral> GetConnectedPeripherals()
        => this.peripherals.Where(x => x.Value.Status == ConnectionState.Connected).Select(x => x.Value);


    public IPeripheral? GetKnownPeripheral(string peripheralUuid)
        => this.peripherals.Values.FirstOrDefault(x => x.Uuid.Equals(peripheralUuid, StringComparison.InvariantCultureIgnoreCase));


    public IObservable<AccessState> RequestAccess() => this.GetRadio().Select(x => x.GetAccessStatus());


    public IObservable<ScanResult> Scan(ScanConfig? scanConfig = null) => this.RequestAccess()
        .Do(access =>
        {
            if (access != AccessState.Available)
                throw new PermissionException("BluetoothLE", access);
        })
        .Select(_ => this.CreateScanner(scanConfig))
        .Switch()
        .SelectMany(args => Observable.FromAsync(async ct =>
        {
            var peripheral = this.GetOrCreatePeripheral(args.BluetoothAddress);
            if (peripheral == null)
            {
                if (!this.pendingPeripheralResolves.TryAdd(args.BluetoothAddress, 0))
                    return null;

                try
                {
                    var btDevice = await BluetoothLEDevice.FromBluetoothAddressAsync(args.BluetoothAddress).AsTask(ct).ConfigureAwait(false);
                    if (btDevice != null)
                        peripheral = this.GetPeripheral(btDevice);
                }
                finally
                {
                    this.pendingPeripheralResolves.TryRemove(args.BluetoothAddress, out _);
                }
            }

            if (peripheral == null)
                return null;

            var adData = new AdvertisementData(args);
            return new ScanResult(peripheral, args.RawSignalStrengthInDBm, adData);
        }))
        .Where(x => x != null)
        .Select(x => x!);


    public void StopScan()
    {
        BluetoothLEAdvertisementWatcher? watcherToStop = null;

        lock (this.scanSyncLock)
        {
            watcherToStop = this.watcher;
            this.watcher = null;
            this.activeScanGeneration = 0;
            this.IsScanning = false;
            this.lastScanStoppedAtUtc = DateTimeOffset.UtcNow;
        }

        watcherToStop?.Stop();
    }


    IObservable<BluetoothLEAdvertisementReceivedEventArgs> CreateScanner(ScanConfig? config)
        => Observable.Create<BluetoothLEAdvertisementReceivedEventArgs>(ob =>
        {
            BluetoothLEAdvertisementWatcher watcher;
            int generation;
            DateTimeOffset startNotBeforeUtc;
            var startCts = new CancellationTokenSource();

            lock (this.scanSyncLock)
            {
                if (this.IsScanning)
                    throw new InvalidOperationException("There is already an active scan");

                this.Clear();
                config ??= new ScanConfig();

                watcher = new BluetoothLEAdvertisementWatcher();
                this.watcher = watcher;
                this.IsScanning = true;
                generation = unchecked(++this.scanGeneration);
                this.activeScanGeneration = generation;
                startNotBeforeUtc = this.lastScanStoppedAtUtc + ScanRestartCooldown;
            }

            if (config.ServiceUuids.Length > 0)
            {
                foreach (var serviceUuid in config.ServiceUuids)
                    watcher.AdvertisementFilter.Advertisement.ServiceUuids.Add(Utils.ToUuidType(serviceUuid));
            }

            watcher.ScanningMode = BluetoothLEScanningMode.Active;
            watcher.SignalStrengthFilter.InRangeThresholdInDBm = -80;
            watcher.SignalStrengthFilter.OutOfRangeThresholdInDBm = -90;
            watcher.SignalStrengthFilter.OutOfRangeTimeout = TimeSpan.FromSeconds(2);
            watcher.SignalStrengthFilter.SamplingInterval = TimeSpan.FromMilliseconds(500);
            var handler = new TypedEventHandler<BluetoothLEAdvertisementWatcher, BluetoothLEAdvertisementReceivedEventArgs>(
                (sender, args) =>
                {
                    lock (this.scanSyncLock)
                    {
                        if (!ReferenceEquals(this.watcher, sender) || this.activeScanGeneration != generation || !this.IsScanning)
                            return;
                    }

                    ob.OnNext(args);
                }
            );

            var stoppedHandler = new TypedEventHandler<BluetoothLEAdvertisementWatcher, BluetoothLEAdvertisementWatcherStoppedEventArgs>(
                (sender, args) =>
                {
                    var isActiveScan = false;
                    lock (this.scanSyncLock)
                    {
                        if (ReferenceEquals(this.watcher, sender) && this.activeScanGeneration == generation)
                        {
                            this.watcher = null;
                            this.activeScanGeneration = 0;
                            this.IsScanning = false;
                            this.lastScanStoppedAtUtc = DateTimeOffset.UtcNow;
                            isActiveScan = true;
                        }
                    }

                    if (isActiveScan && args.Error != BluetoothError.Success)
                        ob.OnError(new BleException($"Scan stopped with error: {args.Error}"));
                }
            );

            watcher.Received += handler;
            watcher.Stopped += stoppedHandler;
            _ = Task.Run(async () =>
            {
                try
                {
                    var delay = startNotBeforeUtc - DateTimeOffset.UtcNow;
                    if (delay > TimeSpan.Zero)
                        await Task.Delay(delay, startCts.Token).ConfigureAwait(false);

                    lock (this.scanSyncLock)
                    {
                        if (!ReferenceEquals(this.watcher, watcher) || this.activeScanGeneration != generation || !this.IsScanning)
                            return;
                    }

                    watcher.Start();
                }
                catch (OperationCanceledException)
                {
                }
                catch (Exception ex)
                {
                    ob.OnError(ex);
                }
            }, startCts.Token);

            return () =>
            {
                startCts.Cancel();
                startCts.Dispose();
                watcher.Received -= handler;
                watcher.Stopped -= stoppedHandler;

                var shouldStop = false;
                lock (this.scanSyncLock)
                {
                    if (ReferenceEquals(this.watcher, watcher) && this.activeScanGeneration == generation)
                    {
                        this.watcher = null;
                        this.activeScanGeneration = 0;
                        this.IsScanning = false;
                        this.lastScanStoppedAtUtc = DateTimeOffset.UtcNow;
                        shouldStop = true;
                    }
                }

                if (shouldStop)
                    watcher.Stop();
            };
        });


    public IObservable<Radio?> GetRadio() => Observable.Create<Radio?>(ob =>
    {
        IDisposable? sub = null;

        if (this.SelectedRadio != null)
        {
            ob.Respond(this.SelectedRadio!);
        }
        else
        {
            sub = this.GetRadios().Subscribe(x =>
            {
                this.SelectedRadio = x.FirstOrDefault();
                ob.Respond(this.SelectedRadio);
            });
        }
        return () => sub?.Dispose();
    });


    public IObservable<IReadOnlyList<Radio>> GetRadios() => Observable.FromAsync<IReadOnlyList<Radio>>(async ct =>
    {
        var list = new List<Radio>();
        var peripherals = await DeviceInformation
            .FindAllAsync(BluetoothAdapter.GetDeviceSelector())
            .AsTask(ct)
            .ConfigureAwait(false);

        foreach (var dev in peripherals)
        {
            var native = await BluetoothAdapter.FromIdAsync(dev.Id);
            if (native.IsLowEnergySupported)
            {
                var radio = await native.GetRadioAsync();
                list.Add(radio);
            }
        }
        return list;
    });


    readonly ConcurrentDictionary<ulong, Peripheral> peripherals = new();

    Peripheral? GetOrCreatePeripheral(ulong bluetoothAddress)
    {
        this.peripherals.TryGetValue(bluetoothAddress, out var peripheral);
        return peripheral;
    }


    Peripheral GetPeripheral(BluetoothLEDevice native)
    {
        var peripheral = this.peripherals.GetOrAdd(
            native.BluetoothAddress,
            _ => new Peripheral(this, native, this.services.GetRequiredService<ILogger<IPeripheral>>())
        );
        return peripheral;
    }


    internal void FirePeripheralStateChanged(Peripheral peripheral)
    {
        this.services.RunDelegates<IBleDelegate>(
            x => x.OnPeripheralStateChanged(peripheral),
            this.logger
        );
    }


    internal void RemovePeripheral(Peripheral peripheral)
    {
        this.peripherals.TryRemove(peripheral.BluetoothAddress, out _);
    }


    void Clear() => this.peripherals
        .Where(x => x.Value.Status != ConnectionState.Connected)
        .ToList()
        .ForEach(x => this.peripherals.TryRemove(x.Key, out _));
}

