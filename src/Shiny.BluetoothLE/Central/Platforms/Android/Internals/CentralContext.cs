using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Android.Bluetooth;
using Android.Bluetooth.LE;
using ScanMode = Android.Bluetooth.LE.ScanMode;
using Shiny.Logging;


namespace Shiny.BluetoothLE.Central.Internals
{
    public class CentralContext
    {
        readonly ConcurrentDictionary<string, Peripheral> devices;
        readonly Subject<NamedMessage<Peripheral>> peripheralSubject;
        readonly Lazy<IBleCentralDelegate> sdelegate;
        readonly IMessageBus messageBus;
        LollipopScanCallback? callbacks;


        public CentralContext(IServiceProvider serviceProvider,
                              AndroidContext context,
                              IMessageBus messageBus,
                              BleCentralConfiguration config)
        {
            this.Android = context;
            this.Configuration = config;
            this.Manager = context.GetBluetooth();

            this.sdelegate = new Lazy<IBleCentralDelegate>(() => serviceProvider.Resolve<IBleCentralDelegate>());
            this.devices = new ConcurrentDictionary<string, Peripheral>();
            this.peripheralSubject = new Subject<NamedMessage<Peripheral>>();
            this.messageBus = messageBus;

            this.StatusChanged
                .Skip(1)
                .SubscribeAsync(status => Log.SafeExecute(
                    async () => await this.sdelegate.Value?.OnAdapterStateChanged(status)
                ));
        }


        public AccessState Status => this.Manager.GetAccessState();
        public IObservable<AccessState> StatusChanged => this.messageBus
            .Listener<State>()
            .Select(x => x.FromNative())
            .StartWith(this.Status);

        public IObservable<NamedMessage<Peripheral>> PeripheralEvents => this.peripheralSubject;

        public BleCentralConfiguration Configuration { get; }
        public BluetoothManager Manager { get; }
        public AndroidContext Android { get; }


        internal void DeviceEvent(string eventName, BluetoothDevice device)
        {
            var peripheral = this.GetDevice(device);
            if (eventName.Equals(BluetoothDevice.ActionAclConnected))
                this.sdelegate.Value?.OnConnected(peripheral);

            this.peripheralSubject.OnNext(new NamedMessage<Peripheral>(eventName, peripheral));
            //case BluetoothDevice.ActionAclConnected:
            //case BluetoothDevice.ActionAclDisconnected:
            //case BluetoothDevice.ActionBondStateChanged:
            //case BluetoothDevice.ActionNameChanged:
            //case BluetoothDevice.ActionPairingRequest:
        }


        public IObservable<IPeripheral> ListenForMe(string eventName, IPeripheral me) => this
            .peripheralSubject
            .Where(x =>
                x.Name.Equals(eventName) &&
                x.Arg.Equals(me)
            )
            .Select(x => x.Arg);


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


        public IObservable<IScanResult> Scan(ScanConfig config) => Observable.Create<ScanResult>(ob =>
        {
            this.devices.Clear();

            this.callbacks = new LollipopScanCallback(sr =>
            {
                var scanResult = this.ToScanResult(sr.Device, sr.Rssi, new AdvertisementData(sr));
                ob.OnNext(scanResult);
            });

            var builder = new ScanSettings.Builder();
            var scanMode = this.ToNative(config.ScanType);
            builder.SetScanMode(scanMode);

            var scanFilters = new List<ScanFilter>();
            if (config.ServiceUuids != null && config.ServiceUuids.Count > 0)
            {
                foreach (var guid in config.ServiceUuids)
                {
                    var uuid = guid.ToParcelUuid();
                    scanFilters.Add(new ScanFilter.Builder()
                        .SetServiceUuid(uuid)
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
                //case BleScanType.Background:
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