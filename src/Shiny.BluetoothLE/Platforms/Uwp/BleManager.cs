using System;
using System.Collections.Generic;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Advertisement;
using Windows.Devices.Radios;
using Windows.Foundation;
using Windows.Devices.Enumeration;
using Shiny.BluetoothLE.Internals;


namespace Shiny.BluetoothLE
{
    public class BleManager : AbstractBleManager,
                              ICanControlAdapterState,
                              ICanSeePairedPeripherals
    {
        readonly ManagerContext context;
        readonly Subject<bool> scanSubject;
        BluetoothAdapter native;
        Radio radio;


        public BleManager(ManagerContext context)
        {
            this.scanSubject = new Subject<bool>();
            this.context = context;
        }


        //public CentralManager(BluetoothAdapter native, Radio radio) : this()
        //{
        //    this.native = native;
        //    this.radio = radio;
        //    this.AdapterName = radio.Name;
        //}


        public override IObservable<AccessState> RequestAccess() => this.GetRadio()
            .Catch((ArgumentException ex) => Observable.Return<Radio>(null))
            .Select(x => x == null ? AccessState.NotSupported : this.Status);


        public override bool IsScanning { get; protected set; }


        public override AccessState Status
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


        public override IObservable<IPeripheral?> GetKnownPeripheral(string peripheralUuid) => Observable.FromAsync(async ct =>
        {
            var mac = Guid.Parse(peripheralUuid).ToBluetoothAddress();
            var per = this.context.GetPeripheral(mac);

            if (per == null)
            {
                var native = await BluetoothLEDevice.FromBluetoothAddressAsync(mac).AsTask(ct);
                if (native != null)
                    per = this.context.AddOrGetPeripheral(native);
            }

            return per;
        });


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


        public override void StopScan() => this.scanSubject.OnNext(false);


        public override IObservable<AccessState> WhenStatusChanged() => Observable.Create<AccessState>(ob =>
        {
            Radio? r = null;
            var handler = new TypedEventHandler<Radio, object>((sender, args) =>
                ob.OnNext(this.Status)
            );
            var sub = this
                .GetRadio()
                .Subscribe(
                    rdo =>
                    {
                        r = rdo;
                        ob.OnNext(this.Status);
                        r.StateChanged += handler;
                    },
                    ob.OnError
                );

            return () =>
            {
                sub.Dispose();
                if (r != null)
                    r.StateChanged -= handler;
            };
        })
        .StartWith(this.Status);


        public IObservable<IEnumerable<IPeripheral>> GetPairedPeripherals() => this.GetDevices(
            BluetoothLEDevice.GetDeviceSelectorFromPairingState(true)
        );


        public IObservable<bool> SetAdapterState(bool enable) => this
            .GetRadio()
            .Select(x => Observable.FromAsync(async () =>
            {
                var state = enable? RadioState.On: RadioState.Off;
                return await this.radio.SetStateAsync(state);
            }))
            .Switch()
            .Select(x => x == RadioAccessStatus.Allowed);


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
    }
}