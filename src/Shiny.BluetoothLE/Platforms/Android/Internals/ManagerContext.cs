using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Android.Bluetooth;
using Android.Bluetooth.LE;
using Android.Content;
using Android.OS;
using Microsoft.Extensions.Logging;
using ScanMode = Android.Bluetooth.LE.ScanMode;
using Observable = System.Reactive.Linq.Observable;

namespace Shiny.BluetoothLE.Internals;


public class ManagerContext : IShinyStartupTask
{
    readonly ConcurrentDictionary<string, Peripheral> devices = new();
    readonly Subject<(Intent Intent, Peripheral Peripheral)> peripheralSubject = new();
    readonly ILogger logger;
    LollipopScanCallback? callbacks;


    public ManagerContext(
        AndroidPlatform platform,
        IServiceProvider serviceProvider,
        AndroidBleConfiguration config,
        ILoggerFactory loggerFactory,
        ILogger<ManagerContext> logger
    )
    {
        this.Android = platform;
        this.Configuration = config;
        this.Services = serviceProvider;
        this.Logging = loggerFactory;
        this.logger = logger;
        this.Manager = platform.GetSystemService<BluetoothManager>(Context.BluetoothService);
    }


    public AndroidBleConfiguration Configuration { get; }
    public BluetoothManager Manager { get; }
    public AndroidPlatform Android { get; }
    public ILoggerFactory Logging { get; }

    public void Start()
    {
        this.Android.RegisterBroadcastReceiver<ShinyBleBroadcastReceiver>(
            BluetoothDevice.ActionNameChanged,
            BluetoothDevice.ActionBondStateChanged,
            BluetoothDevice.ActionPairingRequest,
            BluetoothDevice.ActionAclConnected
        );
        ShinyBleBroadcastReceiver
            .WhenBleEvent()
            .Subscribe(intent => this.DeviceEvent(intent));

        this.Android.RegisterBroadcastReceiver<ShinyBleAdapterStateBroadcastReceiver>(
            BluetoothAdapter.ActionStateChanged
        );

        // TODO: convert this to an async func
        ShinyBleAdapterStateBroadcastReceiver
            .WhenStateChanged()
            .Where(x =>
                x != State.TurningOn &&
                x != State.TurningOff
            )
            .Select(x => x.FromNative())
            .SubscribeAsync(status =>
                this.Services.RunDelegates<IBleDelegate>(
                    del => del.OnAdapterStateChanged(status),
                    this.logger
                )
            );
    }


    public IServiceProvider Services { get; }
    public AccessState Status => this.Manager.GetAccessState();
    public IObservable<AccessState> StatusChanged() => ShinyBleAdapterStateBroadcastReceiver
        .WhenStateChanged()
        .Select(x => x.FromNative())
        .StartWith(this.Status);


    public IObservable<(Intent Intent, Peripheral Peripheral)> PeripheralEvents
        => this.peripheralSubject;


    async void DeviceEvent(Intent intent)
    {
        try
        {
            var device = (BluetoothDevice)intent.GetParcelableExtra(BluetoothDevice.ExtraDevice)!;
            var peripheral = this.GetDevice(device);

            if (intent.Action?.Equals(BluetoothDevice.ActionAclConnected) ?? false)
            {
                await this.Services
                    .RunDelegates<IBleDelegate>(
                        x => x.OnConnected(peripheral),
                        this.logger
                    )
                    .ConfigureAwait(false);
            }
            this.peripheralSubject.OnNext((intent, peripheral));
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "DeviceEvent error");
        }
    }


    public IObservable<Intent> ListenForMe(Peripheral me) => this
        .peripheralSubject
        .Where(x => x.Peripheral.Native.Address!.Equals(me.Native.Address))
        .Select(x => x.Intent);


    public IObservable<Intent> ListenForMe(string eventName, Peripheral me) => this
        .ListenForMe(me)
        .Where(intent => intent.Action?.Equals(
            eventName,
            StringComparison.InvariantCultureIgnoreCase
        ) ?? false);


    public Peripheral GetDevice(BluetoothDevice btDevice) => this.devices.GetOrAdd(
        btDevice.Address!,
        x => new Peripheral(this, btDevice)
    );


    public IEnumerable<Peripheral> GetConnectedDevices()
    {
        var nativeDevices = this.Manager.GetDevicesMatchingConnectionStates(ProfileType.Gatt, new[]
        {
            (int) ProfileState.Connecting,
            (int) ProfileState.Connected
        });
        foreach (var native in nativeDevices)
            yield return this.GetDevice(native);
    }


    public void Clear()
    {
        var connectedDevices = this.GetConnectedDevices().ToList();
        this.devices.Clear();
        foreach (var dev in connectedDevices)
            this.devices.TryAdd(dev.Native.Address!, dev);
    }


    public IObservable<ScanResult> Scan(ScanConfig config) => Observable.Create<ScanResult>(ob =>
    {
        this.devices.Clear();

        this.callbacks = new LollipopScanCallback(
            sr =>
            {
                var scanResult = this.ToScanResult(sr.Device!, sr.Rssi, new AdvertisementData(sr));
                ob.OnNext(scanResult);
            },
            errorCode => ob.OnError(new BleException("Error during scan: " + errorCode.ToString()))
        );

        AndroidScanConfig cfg = null!;
        if (config == null)
            cfg = new();
        else if (config is AndroidScanConfig cfg1)
            cfg = cfg1;
        else
            cfg = new AndroidScanConfig(ServiceUuids: config.ServiceUuids);
        
        var builder = new ScanSettings.Builder();
        builder.SetScanMode(cfg.ScanMode);

        var scanFilters = new List<ScanFilter>();
        if (cfg.ServiceUuids.Length > 0)
        {
            foreach (var uuid in cfg.ServiceUuids)
            {
                var fullUuid = Utils.ToUuidType(uuid);
                var parcel = new ParcelUuid(fullUuid);
                scanFilters.Add(new ScanFilter.Builder()
                    .SetServiceUuid(parcel)
                    .Build()
                );
            }
        }

        if (cfg.UseScanBatching && this.Manager.Adapter!.IsOffloadedScanBatchingSupported)
            builder.SetReportDelay(100);

        this.Manager.Adapter!.BluetoothLeScanner!.StartScan(
            scanFilters,
            builder.Build(),
            this.callbacks
        );

        return () => this.Manager.Adapter.BluetoothLeScanner?.StopScan(this.callbacks);
    });


    public void StopScan()
    {
        if (this.callbacks == null)
            return;

        this.Manager.Adapter!.BluetoothLeScanner?.StopScan(this.callbacks);
        this.callbacks = null;
    }


    protected ScanResult ToScanResult(BluetoothDevice native, int rssi, IAdvertisementData ad)
    {
        var dev = this.GetDevice(native);
        var result = new ScanResult(dev, rssi, ad);
        return result;
    }
}
