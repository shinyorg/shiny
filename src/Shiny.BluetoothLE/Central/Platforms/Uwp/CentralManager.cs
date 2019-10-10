using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Advertisement;
using Windows.Devices.Radios;
using Windows.Foundation;
using Windows.Devices.Enumeration;


namespace Shiny.BluetoothLE.Central
{
    public class CentralManager : AbstractCentralManager,
                                  ICanControlAdapterState,
                                  ICanSeePairedPeripherals
    {
        readonly CentralContext context;
        readonly Subject<bool> scanSubject;
        BluetoothAdapter native;
        Radio radio;


        public CentralManager(CentralContext context)
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


        // TODO: check appmanifest
        public override IObservable<AccessState> RequestAccess() => Observable.Return(AccessState.Available);
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


        public override IObservable<IPeripheral> GetKnownPeripheral(Guid deviceId) => Observable.FromAsync(async ct =>
        {
            var mac = deviceId.ToBluetoothAddress();
            var per = this.context.GetPeripheral(mac);

            if (per == null)
            {
                var native = await BluetoothLEDevice.FromBluetoothAddressAsync(mac).AsTask(ct);
                if (native != null)
                    per = this.context.AddOrGetPeripheral(native);
            }

            return per;
        });


        public override IObservable<IEnumerable<IPeripheral>> GetConnectedPeripherals(Guid? serviceUuid = null) => this.GetDevices(
            BluetoothLEDevice.GetDeviceSelectorFromConnectionStatus(BluetoothConnectionStatus.Connected)
        );


        public override IObservable<IScanResult> Scan(ScanConfig config)
        {
            if (this.IsScanning)
                throw new ArgumentException("There is already an active scan");

            return Observable.Create<IScanResult>(ob =>
            {
                this.IsScanning = true;
                this.context.Clear();

                var sub = this
                    .WhenRadioReady()
                    .Where(rdo => rdo != null)
                    .Select(_ => this.CreateScanner(config))
                    .Switch()
                    .Subscribe(
                        async args => // CAREFUL
                        {
                            var device = this.context.GetPeripheral(args.BluetoothAddress);
                            if (device == null)
                            {
                                var btDevice = await BluetoothLEDevice.FromBluetoothAddressAsync(args.BluetoothAddress);
                                if (btDevice != null)
                                    device = this.context.AddOrGetPeripheral(btDevice);
                            }
                            if (device != null)
                            {
                                var adData = new AdvertisementData(args);
                                var scanResult = new ScanResult(device, args.RawSignalStrengthInDBm, adData);
                                ob.OnNext(scanResult);
                            }
                        },
                        ob.OnError
                    );

                var stopSub = this.scanSubject.Subscribe(_ =>
                {
                    this.IsScanning = false;
                    sub?.Dispose();
                    ob.OnCompleted();
                });

                return () =>
                {
                    this.IsScanning = false;
                    sub?.Dispose();
                    stopSub?.Dispose();
                };
            });
        }


        public override void StopScan() => this.scanSubject.OnNext(false);


        public override IObservable<AccessState> WhenStatusChanged() => Observable.Create<AccessState>(ob =>
        {
            Radio r = null;
            var handler = new TypedEventHandler<Radio, object>((sender, args) =>
                ob.OnNext(this.Status)
            );
            var sub = this
                .WhenRadioReady()
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


        public async void SetAdapterState(bool enable)
        {
            var state = enable ? RadioState.On : RadioState.Off;
            await this.radio.SetStateAsync(state);
        }


        IObservable<BluetoothLEAdvertisementReceivedEventArgs> CreateScanner(ScanConfig config)
             => Observable.Create<BluetoothLEAdvertisementReceivedEventArgs>(ob =>
             {
                 this.context.Clear();
                 config = config ?? new ScanConfig { ScanType = BleScanType.Balanced };

                 var adWatcher = new BluetoothLEAdvertisementWatcher();
                 if (config.ServiceUuids != null)
                     foreach (var serviceUuid in config.ServiceUuids)
                         adWatcher.AdvertisementFilter.Advertisement.ServiceUuids.Add(serviceUuid);

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


        IObservable<Radio> WhenRadioReady() => Observable.FromAsync(async ct =>
        {
            if (this.radio != null)
                return this.radio;

            this.native = await BluetoothAdapter.GetDefaultAsync().AsTask(ct);
            if (this.native == null)
                throw new ArgumentException("No bluetooth adapter found");

            this.radio = await this.native.GetRadioAsync().AsTask(ct);
            this.AdapterName = this.radio.Name;
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