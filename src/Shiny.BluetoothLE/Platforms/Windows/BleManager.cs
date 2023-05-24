using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

using Windows.Devices.Bluetooth;

namespace Shiny.BluetoothLE;


public partial class BleManager : IBleManager
{
    readonly ConcurrentDictionary<ulong, IPeripheral> peripherals = new();


    public bool IsScanning { get; private set; }

    public IEnumerable<IPeripheral> GetConnectedPeripherals() => throw new NotImplementedException();
    public IPeripheral? GetKnownPeripheral(string peripheralUuid) => throw new NotImplementedException();
    //public override IObservable<IPeripheral?> GetKnownPeripheral(string peripheralUuid) => Observable.FromAsync(async ct =>
    //{
    //    var mac = Guid.Parse(peripheralUuid).ToBluetoothAddress();
    //    var per = this.context.GetPeripheral(mac);

    //    if (per == null)
    //    {
    //        var native = await BluetoothLEDevice.FromBluetoothAddressAsync(mac).AsTask(ct);
    //        if (native != null)
    //            per = this.context.AddOrGetPeripheral(native);
    //    }

    //    return per;
    //});


    public IObservable<AccessState> RequestAccess() => throw new NotImplementedException();
    public IObservable<ScanResult> Scan(ScanConfig? scanConfig = null) => throw new NotImplementedException();
    public void StopScan() => throw new NotImplementedException();



    IPeripheral GetPeripheral(BluetoothLEDevice native)
    {
        var peripheral = this.peripherals.GetOrAdd(native.BluetoothAddress, id => new Peripheral(null, native));
        return peripheral;
    }


    void Clear() => this.peripherals
        .Where(x => x.Value.Status != ConnectionState.Connected)
        .ToList()
        .ForEach(x => this.peripherals.TryRemove(x.Key, out _));

    //        public IObservable<IAdapter> FindAdapters() => Observable.Create<IAdapter>(async ob =>
    //        {
    //            var peripherals = await DeviceInformation.FindAllAsync(BluetoothAdapter.GetDeviceSelector());
    //            foreach (var dev in peripherals)
    //            {
    //                Log.Info(BleLogCategory.Adapter, "found - {dev.Name} ({dev.Kind} - {dev.Id})");

    //                var native = await BluetoothAdapter.FromIdAsync(dev.Id);
    //                if (native.IsLowEnergySupported)
    //                {
    //                    var radio = await native.GetRadioAsync();
    //                    var adapter = new Adapter(native, radio);
    //                    ob.OnNext(adapter);
    //                }
    //            }
    //            ob.OnCompleted();
    //            return Disposable.Empty;
    //        });



    //public override IObservable<AccessState> WhenStatusChanged() => Observable.Create<AccessState>(ob =>
    //{
    //    Radio? r = null;
    //    var handler = new TypedEventHandler<Radio, object>((sender, args) =>
    //        ob.OnNext(this.Status)
    //    );
    //    var sub = this
    //        .GetRadio()
    //        .Subscribe(
    //            rdo =>
    //            {
    //                r = rdo;
    //                ob.OnNext(this.Status);
    //                r.StateChanged += handler;
    //            },
    //            ob.OnError
    //        );

    //    return () =>
    //    {
    //        sub.Dispose();
    //        if (r != null)
    //            r.StateChanged -= handler;
    //    };
    //})
    //.StartWith(this.Status);
}


/*
        public override IObservable<AccessState> RequestAccess() => this.GetRadio()
            .Catch((ArgumentException ex) => Observable.Return<Radio>(null))
            .Select(x => x == null ? AccessState.NotSupported : this.Status);


        AccessState Status
        {
            get
            {
                if (this.radio == null)
                    return AccessState.Unknown;

                switch (this.radio.State)
                {
                    case RadioState.Disabled:
                    case RadioState.Off:
                        return AccessState.Disabled;

                    case RadioState.Unknown:
                        return AccessState.Unknown;

                    default:
                        return AccessState.Available;
                }
            }
        }


        


        public override IObservable<IEnumerable<IPeripheral>> GetConnectedPeripherals(string? serviceUuid = null) => this.GetDevices(
            BluetoothLEDevice.GetDeviceSelectorFromConnectionStatus(BluetoothConnectionStatus.Connected)
        );


        public override IObservable<ScanResult> Scan(ScanConfig? config = null)
        {
            if (this.IsScanning)
                throw new ArgumentException("There is already an active scan");

            return this.RequestAccess()
                .Do(result =>
                {
                    if (result != AccessState.Available)
                        throw new PermissionException("BluetoothLE", result);

                    this.IsScanning = true;
                    this.context.Clear();
                })
                .Select(_ => this.GetRadio())
                .Switch()
                .Select(_ => this.CreateScanner(config))
                .Switch()
                .Select(args => Observable.FromAsync(async ct =>
                {
                    var device = this.context.GetPeripheral(args.BluetoothAddress);
                    if (device == null)
                    {
                        var btDevice = await BluetoothLEDevice.FromBluetoothAddressAsync(args.BluetoothAddress);
                        if (btDevice != null)
                            device = this.context.AddOrGetPeripheral(btDevice);
                    }
                    ScanResult? scanResult = null;
                    if (device != null)
                    {
                        var adData = new AdvertisementData(args);
                        scanResult = new ScanResult(device, args.RawSignalStrengthInDBm, adData);
                    }
                    return scanResult;
                }))
                .Switch()
                .Where(x => x != null)
                .Finally(() => this.IsScanning = false);
        }


        IObservable<BluetoothLEAdvertisementReceivedEventArgs> CreateScanner(ScanConfig? config)
             => Observable.Create<BluetoothLEAdvertisementReceivedEventArgs>(ob =>
             {
                 this.context.Clear();
                 config ??= new ScanConfig { ScanType = BleScanType.Balanced };

                 var adWatcher = new BluetoothLEAdvertisementWatcher();
                 if (config.ServiceUuids != null)
                 {
                     foreach (var serviceUuid in config.ServiceUuids)
                     {
                         adWatcher.AdvertisementFilter.Advertisement.ServiceUuids.Add(Utils.ToUuidType(serviceUuid));
                     }
                }

                 switch (config.ScanType)
                 {
                     case BleScanType.Balanced:
                         adWatcher.ScanningMode = BluetoothLEScanningMode.Active;
                         break;

                     //case BleScanType.Background:
                     case BleScanType.LowLatency:
                     case BleScanType.LowPowered:
                         adWatcher.ScanningMode = BluetoothLEScanningMode.Passive;
                         break;
                 }
                 var handler = new TypedEventHandler<BluetoothLEAdvertisementWatcher, BluetoothLEAdvertisementReceivedEventArgs>
                     ((sender, args) => ob.OnNext(args)
                 );

                 adWatcher.Received += handler;
                 adWatcher.Start();

                 return () =>
                 {
                     adWatcher.Stop();
                     adWatcher.Received -= handler;
                 };
             });


        IObservable<Radio> GetRadio() => Observable.FromAsync(async ct =>
        {
            if (this.radio != null)
                return this.radio;

            this.native = await BluetoothAdapter.GetDefaultAsync().AsTask(ct);
            if (this.native == null)
                throw new ArgumentException("No bluetooth adapter found");

            this.radio = await this.native.GetRadioAsync().AsTask(ct);
            return this.radio;
        });


        IObservable<IEnumerable<IPeripheral>> GetDevices(string selector) => Observable.FromAsync(async ct =>
        {
            string[] requestedProperties = { "System.Devices.ContainerId" };
            var devices = await DeviceInformation
                .FindAllAsync(
                    selector,
                    requestedProperties,
                    DeviceInformationKind.AssociationEndpoint
                )
                .AsTask(ct);

            var results = new List<IPeripheral>();
            foreach (var deviceInfo in devices)
            {
                var native = await BluetoothLEDevice.FromIdAsync(deviceInfo.Id).AsTask(ct);
                var wrap = this.context.AddOrGetPeripheral(native);
                results.Add(wrap);
            }

            return results;
        });

 */