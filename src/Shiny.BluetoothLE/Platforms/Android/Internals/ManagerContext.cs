using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using Android.Bluetooth;
using Android.Bluetooth.LE;
using Android.Content;
using Android.OS;
using Java.Util;
using Shiny.Infrastructure;
using ScanMode = Android.Bluetooth.LE.ScanMode;
using Observable = System.Reactive.Linq.Observable;


namespace Shiny.BluetoothLE.Internals
{
    public class ManagerContext
    {
        public const string BlePairingFailed = nameof(BlePairingFailed);
        readonly ConcurrentDictionary<string, Peripheral> devices;
        readonly Subject<NamedMessage<Peripheral>> peripheralSubject;
        readonly ShinyCoreServices services;
        LollipopScanCallback? callbacks;


        public ManagerContext(ShinyCoreServices services, BleConfiguration config)
        {
            this.Configuration = config;
            this.services = services;

            this.devices = new ConcurrentDictionary<string, Peripheral>();
            this.peripheralSubject = new Subject<NamedMessage<Peripheral>>();
            this.Manager = services.Android.GetBluetooth();
            //this.StatusChanged
            //    .Skip(1)
            //    .SubscribeAsync(status => Log.SafeExecute(
            //        async () => await this.sdelegate.Value?.OnAdapterStateChanged(status)
            //    ));
        }


        public IServiceProvider Services => this.services.Services;
        public AccessState Status => this.Manager.GetAccessState();
        public IObservable<AccessState> StatusChanged => this.services
            .Bus
            .Listener<State>()
            .Select(x => x.FromNative())
            .StartWith(this.Status);

        public IObservable<NamedMessage<Peripheral>> PeripheralEvents => this.peripheralSubject;

        public BleConfiguration Configuration { get; }
        public BluetoothManager Manager { get; }
        public IAndroidContext Android => this.services.Android;


        internal async void DeviceEvent(Intent intent)
        {
            var device = (BluetoothDevice)intent.GetParcelableExtra(BluetoothDevice.ExtraDevice);
            var peripheral = this.GetDevice(device);
            var action = intent.Action;

            switch (action)
            {
                case BluetoothDevice.ActionAclConnected:
                    await this.Services.RunDelegates<IBleDelegate>(x => x.OnConnected(peripheral));
                    break;

                case BluetoothDevice.ActionPairingRequest:
                    var pin = peripheral.PairingRequestPin;
                    peripheral.PairingRequestPin = null;

                    if (!pin.IsEmpty())
                    {
                        var bytes = Encoding.UTF8.GetBytes(pin);

                        if (!device.SetPin(bytes))
                            action = BlePairingFailed;
                        //else
                        //{
                        //    Log.Write("BlePairing", "Auto-Pairing PIN was sent successfully apparently");
                        //    //device.SetPairingConfirmation(true);
                        //}
                    }
                    break;
            }
            this.peripheralSubject.OnNext(new NamedMessage<Peripheral>(action, peripheral));
        }


        public IObservable<IPeripheral> ListenForMe(string eventName, Peripheral me) => this
            .peripheralSubject
            .Where(x =>
            {
                if (!x.Arg.Native.Address.Equals(me.Native.Address))
                    return false;

                if (!x.Name.Equals(eventName, StringComparison.CurrentCultureIgnoreCase))
                    return false;

                return true;
            })
            .Select(x => x.Arg);


        public string? GetPairingPinRequestForDevice(BluetoothDevice device)
        {
            string? pin = null;
            var p = this.GetDevice(device);
            if (p != null)
            {
                pin = p.PairingRequestPin;
                p.PairingRequestPin = null;
            }
            return pin;
        }


        public Peripheral GetDevice(BluetoothDevice btDevice) => this.devices.GetOrAdd(
            btDevice.Address,
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
                this.devices.TryAdd(dev.Native.Address, dev);
        }


        public IObservable<ScanResult> Scan(ScanConfig config) => Observable.Create<ScanResult>(ob =>
        {
            this.devices.Clear();

            this.callbacks = new LollipopScanCallback(
                sr =>
                {
                    var scanResult = this.ToScanResult(sr.Device, sr.Rssi, new AdvertisementData(sr));
                    ob.OnNext(scanResult);
                },
                errorCode => ob.OnError(new BleException("Error during scan: " + errorCode.ToString()))
            );

            var builder = new ScanSettings.Builder();
            var scanMode = this.ToNative(config.ScanType);
            builder.SetScanMode(scanMode);

            var scanFilters = new List<ScanFilter>();
            if (config.ServiceUuids != null && config.ServiceUuids.Count > 0)
            {
                foreach (var uuid in config.ServiceUuids)
                {
                    var parcel = new ParcelUuid(UUID.FromString(uuid));
                    scanFilters.Add(new ScanFilter.Builder()
                        .SetServiceUuid(parcel)
                        .Build()
                    );
                }
            }

            if (config.AndroidUseScanBatching && this.Manager.Adapter.IsOffloadedScanBatchingSupported)
                builder.SetReportDelay(100);

            this.Manager.Adapter.BluetoothLeScanner.StartScan(
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

            this.Manager.Adapter.BluetoothLeScanner?.StopScan(this.callbacks);
            this.callbacks = null;
        }


        protected ScanResult ToScanResult(BluetoothDevice native, int rssi, IAdvertisementData ad)
        {
            var dev = this.GetDevice(native);
            var result = new ScanResult(dev, rssi, ad);
            return result;
        }


        protected virtual ScanMode ToNative(BleScanType scanType)
        {
            switch (scanType)
            {
                case BleScanType.LowPowered:
                    return ScanMode.LowPower;

                case BleScanType.Balanced:
                    return ScanMode.Balanced;

                case BleScanType.LowLatency:
                    return ScanMode.LowLatency;

                default:
                    throw new ArgumentException("Invalid BleScanType");
            }
        }
    }
}