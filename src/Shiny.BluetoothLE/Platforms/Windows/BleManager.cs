using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
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
    readonly IServiceProvider services;
    readonly ILogger logger;
    BluetoothLEAdvertisementWatcher? watcher;


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
        .Select(args => Observable.FromAsync(async ct =>
        {
            var peripheral = this.GetOrCreatePeripheral(args.BluetoothAddress);
            if (peripheral == null)
            {
                var btDevice = await BluetoothLEDevice.FromBluetoothAddressAsync(args.BluetoothAddress).AsTask(ct).ConfigureAwait(false);
                if (btDevice != null)
                    peripheral = this.GetPeripheral(btDevice);
            }

            if (peripheral == null)
                return null;

            var adData = new AdvertisementData(args);
            return new ScanResult(peripheral, args.RawSignalStrengthInDBm, adData);
        }))
        .Switch()
        .Where(x => x != null)
        .Select(x => x!);


    public void StopScan()
    {
        this.watcher?.Stop();
        this.watcher = null;
        this.IsScanning = false;
    }


    IObservable<BluetoothLEAdvertisementReceivedEventArgs> CreateScanner(ScanConfig? config)
        => Observable.Create<BluetoothLEAdvertisementReceivedEventArgs>(ob =>
        {
            if (this.IsScanning)
                throw new InvalidOperationException("There is already an active scan");

            this.Clear();
            config ??= new ScanConfig();

            this.watcher = new BluetoothLEAdvertisementWatcher();

            if (config.ServiceUuids.Length > 0)
            {
                foreach (var serviceUuid in config.ServiceUuids)
                    this.watcher.AdvertisementFilter.Advertisement.ServiceUuids.Add(Utils.ToUuidType(serviceUuid));
            }

            this.watcher.ScanningMode = BluetoothLEScanningMode.Active;

            var handler = new TypedEventHandler<BluetoothLEAdvertisementWatcher, BluetoothLEAdvertisementReceivedEventArgs>(
                (sender, args) => ob.OnNext(args)
            );

            var stoppedHandler = new TypedEventHandler<BluetoothLEAdvertisementWatcher, BluetoothLEAdvertisementWatcherStoppedEventArgs>(
                (sender, args) =>
                {
                    if (args.Error != BluetoothError.Success)
                        ob.OnError(new BleException($"Scan stopped with error: {args.Error}"));
                }
            );

            this.watcher.Received += handler;
            this.watcher.Stopped += stoppedHandler;
            this.watcher.Start();
            this.IsScanning = true;

            return () =>
            {
                this.watcher.Received -= handler;
                this.watcher.Stopped -= stoppedHandler;
                this.StopScan();
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


    void Clear() => this.peripherals
        .Where(x => x.Value.Status != ConnectionState.Connected)
        .ToList()
        .ForEach(x => this.peripherals.TryRemove(x.Key, out _));
}
