using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
using Android;
using Android.Bluetooth;
using Android.Bluetooth.LE;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Microsoft.Extensions.Logging;
using SR = Android.Bluetooth.LE.ScanResult;

namespace Shiny.BluetoothLE;


public class BleManager : ScanCallback, IBleManager, IShinyStartupTask
{
    public const string BroadcastReceiverName = "org.shiny.bluetoothle.ShinyBleCentralBroadcastReceiver";

    readonly AndroidPlatform platform;
    readonly AndroidBleConfiguration config;
    readonly ILogger<IBleManager> logger;
    readonly ILogger<IPeripheral> peripheralLogger;

    public BleManager(
        AndroidPlatform platform,
        AndroidBleConfiguration config,
        ILogger<IBleManager> logger,
        ILogger<IPeripheral> peripheralLogger
    )
    {
        this.platform = platform;
        this.config = config;
        this.logger = logger;
        this.peripheralLogger = peripheralLogger;

        this.Native = platform.GetSystemService<BluetoothManager>(Context.BluetoothService);
    }


    public bool IsScanning => this.scanSubj != null;
    public BluetoothManager Native { get; }
    //    public IServiceProvider Services { get; }
    //    public AccessState Status => this.Manager.GetAccessState();


    public void Start()
    {
        ShinyBleBroadcastReceiver.Process = async intent =>
        {
            //this.DeviceEvent(intent)
        };

        this.platform.RegisterBroadcastReceiver<ShinyBleBroadcastReceiver>(
            BluetoothDevice.ActionNameChanged,
            BluetoothDevice.ActionBondStateChanged,
            BluetoothDevice.ActionPairingRequest,
            BluetoothDevice.ActionAclConnected,
            Intent.ActionBootCompleted
        );
        ShinyBleAdapterStateBroadcastReceiver.Process = async intent =>
        {
            //            var newState = (State)intent.GetIntExtra(BluetoothAdapter.ExtraState, -1);
            //            stateSubj.OnNext(newState);

            //        ShinyBleAdapterStateBroadcastReceiver
            //            .WhenStateChanged()
            //            .Where(x =>
            //                x != State.TurningOn &&
            //                x != State.TurningOff
            //            )
            //            .Select(x => x.FromNative())
            //            .SubscribeAsync(status =>
            //                this.Services.RunDelegates<IBleDelegate>(
            //                    del => del.OnAdapterStateChanged(status),
            //                    this.logger
            //                )
            //            );
        };

        this.platform.RegisterBroadcastReceiver<ShinyBleAdapterStateBroadcastReceiver>(
            BluetoothAdapter.ActionStateChanged,
            Intent.ActionBootCompleted
        );
    }

    public IObservable<AccessState> RequestAccess() => Observable.FromAsync(async ct =>
    {
        var versionPermissions = GetPlatformPermissions();

        if (!versionPermissions.All(x => this.platform.IsInManifest(x)))
            return AccessState.NotSetup;

        var results = await this.platform
            .RequestPermissions(versionPermissions)
            .ToTask(ct)
            .ConfigureAwait(false);

        return results.IsSuccess()
            ? this.Native.GetAccessState() // now look at the actual device state
            : AccessState.Denied;
    });


    Subject<ScanResult>? scanSubj;
    public IObservable<ScanResult> Scan(ScanConfig? config = null) => Observable.Create<ScanResult>(ob =>
    {
        if (this.scanSubj != null)
            throw new InvalidOperationException("There is already an active scan");

        this.scanSubj = new();
        this.Clear();

        var disp = this.scanSubj.Subscribe(
            ob.OnNext,
            ob.OnError
        );
        this.StartScan(config);


        return () =>
        {
            this.StopScan();
            disp?.Dispose();
        };

        //        return this
        //            .RequestAccess()
        //            .Do(access =>
        //            {
        //                Assert(access);
        //                this.IsScanning = true;
        //            })
    });


    public void StopScan()
    {
        this.Native.Adapter!.BluetoothLeScanner?.StopScan(this);
        this.scanSubj?.Dispose();
        this.scanSubj = null;
    }


    public IObservable<IEnumerable<IPeripheral>> GetConnectedPeripherals(string? serviceUuid = null) => throw new NotImplementedException();
    public IObservable<IPeripheral?> GetKnownPeripheral(string peripheralUuid) => throw new NotImplementedException();


    public override void OnScanResult([GeneratedEnum] ScanCallbackType callbackType, SR? result)
    {
        if (result != null)
            this.Trigger(result);
    }
   

    public override void OnScanFailed([GeneratedEnum] ScanFailure errorCode)
        => this.scanSubj?.OnError(new InvalidOperationException("Scan Error: " + errorCode));

    
    public override void OnBatchScanResults(IList<SR>? results)
    {
        if (results == null)
            return;

        foreach (var result in results)
            this.Trigger(result);
    }


    readonly ConcurrentDictionary<string, Peripheral> peripherals = new();
    Peripheral GetPeripheral(BluetoothDevice device) => this.peripherals.GetOrAdd(
        device.Address!,
        x => new Peripheral(this, this.platform, device, this.peripheralLogger)
    );


    void Trigger(SR native) => this.scanSubj?.OnNext(this.FromNative(native));
    ScanResult FromNative(SR native)
    {
        var peripheral = this.GetPeripheral(native.Device!);
        var ad = new AdvertisementData(native);
        return new ScanResult(peripheral, native.Rssi, ad);
    }


    void StartScan(ScanConfig? config)
    {
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

        if (cfg.UseScanBatching && this.Native.Adapter!.IsOffloadedScanBatchingSupported)
            builder.SetReportDelay(100);

        this.Native.Adapter!.BluetoothLeScanner!.StartScan(
            scanFilters,
            builder.Build(),
            this
        );
    }


    void Clear()
    {
        //var connectedDevices = this.GetConnectedDevices().ToList();
        this.peripherals.Clear();
        //foreach (var dev in connectedDevices)
        //    this.devices.TryAdd(dev.Native.Address!, dev);
    }


    static string[] GetPlatformPermissions()
    {
        var list = new List<string>();

        if (OperatingSystemShim.IsAndroidVersionAtLeast(31))
        {
            return new[]
            {
                Manifest.Permission.BluetoothScan,
                Manifest.Permission.BluetoothConnect
            };
        }
        return new[]
        {
            Manifest.Permission.Bluetooth,
            Manifest.Permission.BluetoothAdmin,
            Manifest.Permission.AccessFineLocation
        };
    }


    static void Assert(AccessState access)
    {
        if (access == AccessState.NotSetup)
        {
            var permissions = GetPlatformPermissions();
            var msgList = String.Join(", ", permissions);
            throw new InvalidOperationException("Your AndroidManifest.xml is missing 1 or more of the following permissions for this version of Android: " + msgList);
        }
        else if (access != AccessState.Available)
        {
            throw new InvalidOperationException($"Invalid Status: {access}");
        }
    }
}

//    readonly Subject<(Intent Intent, Peripheral Peripheral)> peripheralSubject = new();
//    public override IObservable<IPeripheral?> GetKnownPeripheral(string peripheralUuid)
//    {
//        var address = Guid
//            .Parse(peripheralUuid)
//            .ToByteArray()
//            .Skip(10)
//            .Take(6)
//            .ToArray();

//        var native = this.context.Manager.Adapter!.GetRemoteDevice(address);
//        if (native == null)
//            return Observable.Return<IPeripheral?>(null);

//        var peripheral = this.context.GetDevice(native);
//        return Observable.Return(peripheral);
//    }


//    public override IObservable<IEnumerable<IPeripheral>> GetConnectedPeripherals(string? serviceUuid = null)
//        => Observable.Return(this.context
//            .Manager
//            .GetConnectedDevices(ProfileType.Gatt)
//            .Where(x => x.Type == BluetoothDeviceType.Dual || x.Type == BluetoothDeviceType.Le) // just in case
//            .Select(this.context.GetDevice)
//        );


//    public IObservable<IEnumerable<IPeripheral>> GetPairedPeripherals() => Observable.Return(this.context
//        .Manager
//        .Adapter!
//        .BondedDevices
//        .Where(x => x.Type == BluetoothDeviceType.Dual || x.Type == BluetoothDeviceType.Le)
//        .Select(this.context.GetDevice)
//    );

//    public IObservable<AccessState> StatusChanged() => ShinyBleAdapterStateBroadcastReceiver
//        .WhenStateChanged()
//        .Select(x => x.FromNative())
//        .StartWith(this.Status);


//    public IObservable<(Intent Intent, Peripheral Peripheral)> PeripheralEvents
//        => this.peripheralSubject;


//    async void DeviceEvent(Intent intent)
//    {
//        try
//        {
//            var device = (BluetoothDevice)intent.GetParcelableExtra(BluetoothDevice.ExtraDevice)!;
//            var peripheral = this.GetDevice(device);

//            if (intent.Action?.Equals(BluetoothDevice.ActionAclConnected) ?? false)
//            {
//                await this.Services
//                    .RunDelegates<IBleDelegate>(
//                        x => x.OnConnected(peripheral),
//                        this.logger
//                    )
//                    .ConfigureAwait(false);
//            }
//            this.peripheralSubject.OnNext((intent, peripheral));
//        }
//        catch (Exception ex)
//        {
//            this.logger.LogError(ex, "DeviceEvent error");
//        }
//    }


//    public IObservable<Intent> ListenForMe(Peripheral me) => this
//        .peripheralSubject
//        .Where(x => x.Peripheral.Native.Address!.Equals(me.Native.Address))
//        .Select(x => x.Intent);


//    public IObservable<Intent> ListenForMe(string eventName, Peripheral me) => this
//        .ListenForMe(me)
//        .Where(intent => intent.Action?.Equals(
//            eventName,
//            StringComparison.InvariantCultureIgnoreCase
//        ) ?? false);


//    public IEnumerable<Peripheral> GetConnectedDevices()
//    {
//        var nativeDevices = this.Manager.GetDevicesMatchingConnectionStates(ProfileType.Gatt, new[]
//        {
//            (int) ProfileState.Connecting,
//            (int) ProfileState.Connected
//        });
//        foreach (var native in nativeDevices)
//            yield return this.GetDevice(native);
//    }
