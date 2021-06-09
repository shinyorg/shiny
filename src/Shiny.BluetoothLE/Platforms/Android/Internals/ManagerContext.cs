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
using Shiny.Infrastructure;
using ScanMode = Android.Bluetooth.LE.ScanMode;
using Observable = System.Reactive.Linq.Observable;


namespace Shiny.BluetoothLE.Internals
{
    public class ManagerContext : IShinyStartupTask
    {
        readonly ConcurrentDictionary<string, Peripheral> devices;
        readonly Subject<(Intent Intent, Peripheral Peripheral)> peripheralSubject;
        readonly ShinyCoreServices services;
        readonly ILogger logger;
        LollipopScanCallback? callbacks;


        public ManagerContext(ShinyCoreServices services,
                              BleConfiguration config,
                              ILogger<ManagerContext> logger)
        {
            this.Configuration = config;
            this.services = services;
            this.logger = logger;

            this.devices = new ConcurrentDictionary<string, Peripheral>();
            this.peripheralSubject = new Subject<(Intent Intent, Peripheral Peripheral)>();
            this.Manager = services.Android.GetBluetooth();


        }


        public void Start()
        {
            ShinyBleBroadcastReceiver
                .WhenBleEvent()
                .Subscribe(intent => this.DeviceEvent(intent));

            ShinyBleAdapterStateBroadcastReceiver
                .WhenStateChanged()
                .Subscribe(status =>
                    services.Services.RunDelegates<IBleDelegate>(del => del.OnAdapterStateChanged(status))
                );
        }


        public IServiceProvider Services => this.services.Services;
        public AccessState Status => this.Manager.GetAccessState();
        public IObservable<AccessState> StatusChanged() => ShinyBleAdapterStateBroadcastReceiver
            .WhenStateChanged()
            .StartWith(this.Status);


        public IObservable<(Intent Intent, Peripheral Peripheral)> PeripheralEvents
            => this.peripheralSubject;

        public BleConfiguration Configuration { get; }
        public BluetoothManager Manager { get; }
        public IAndroidContext Android => this.services.Android;


        async void DeviceEvent(Intent intent)
        {
            try
            {
                var device = (BluetoothDevice)intent.GetParcelableExtra(BluetoothDevice.ExtraDevice);
                var peripheral = this.GetDevice(device);

                if (intent.Action.Equals(BluetoothDevice.ActionAclConnected))
                    await this.Services.RunDelegates<IBleDelegate>(x => x.OnConnected(peripheral));

                this.peripheralSubject.OnNext((intent, peripheral));
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "DeviceEvent error");
            }
        }


        public IObservable<Intent> ListenForMe(Peripheral me) => this
            .peripheralSubject
            .Where(x => x.Peripheral.Native.Address.Equals(me.Native.Address))
            .Select(x => x.Intent);


        public IObservable<Intent> ListenForMe(string eventName, Peripheral me) => this
            .ListenForMe(me)
            .Where(intent => intent.Action.Equals(eventName, StringComparison.InvariantCultureIgnoreCase));


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
                    var fullUuid = Utils.ToUuidType(uuid);
                    var parcel = new ParcelUuid(fullUuid);
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