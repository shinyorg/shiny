using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Shiny.BluetoothLE.Peripherals.Internals;
using Android.Bluetooth;
using Android.Bluetooth.LE;


namespace Shiny.BluetoothLE.Peripherals
{
    public class PeripheralManager : IPeripheralManager
    {
        readonly GattServerContext context;
        readonly Dictionary<Guid, GattService> services;
        AdvertisementCallbacks adCallbacks;
        BluetoothGattServer server;


        public PeripheralManager(IAndroidContext context)
        {
            this.context = new GattServerContext(context);
            this.services = new Dictionary<Guid, GattService>();
        }


        public AccessState Status
        {
            get
            {
                //if (Build.VERSION.SdkInt < BuildVersionCodes.JellyBeanMr2)
                //    return AdapterStatus.Unsupported;

                //if (!Application.Context.PackageManager.HasSystemFeature(PackageManager.FeatureBluetoothLe))
                //    return AdapterStatus.Unsupported;

                var ad = this.context.Manager?.Adapter;
                if (ad == null)
                    return AccessState.NotSupported;

                if (!ad.IsEnabled)
                    return AccessState.Disabled;

                switch (ad.State)
                {
                    case State.Off:
                    case State.TurningOff:
                    case State.Disconnecting:
                    case State.Disconnected:
                        return AccessState.Disabled;

                    //case State.Connecting
                    case State.On:
                    case State.Connected:
                        return AccessState.Available;

                    default:
                        return AccessState.Unknown;
                }
            }
        }


        public IObservable<AccessState> WhenStatusChanged() => this.context
            .Context
            .WhenAdapterStatusChanged()
            .Select(_ => this.Status)
            .StartWith(this.Status);


        public bool IsAdvertising => this.adCallbacks != null;
        public IReadOnlyList<IGattService> Services => this.services.Values.Cast<IGattService>().ToArray();


        public Task<IGattService> AddService(Guid uuid, bool primary, Action<IGattServiceBuilder> serviceBuilder)
        {
            if (this.server == null)
                this.server = this.context.CreateServer();

            var service = new GattService(this.context, uuid, primary);
            serviceBuilder(service);
            this.services.Add(uuid, service);
            return Task.FromResult<IGattService>(service);
        }


        public void ClearServices()
        {
            this.services.Clear();
            this.server.ClearServices();
            this.Cleanup();
        }


        public void RemoveService(Guid serviceUuid)
        {
            var s = this.services[serviceUuid];
            this.server.RemoveService(s.Native);
            this.services.Remove(serviceUuid);
            if (this.services.Count == 0)
                this.Cleanup();
        }


        public Task StartAdvertising(AdvertisementData adData = null)
        {
            //            //if (!CrossBleAdapter.AndroidConfiguration.IsServerSupported)
            //            //    throw new BleException("BLE Advertiser needs API Level 23+");

            this.adCallbacks = new AdvertisementCallbacks();

            var settings = new AdvertiseSettings.Builder()
                .SetAdvertiseMode(AdvertiseMode.Balanced)
                .SetConnectable(true);

            var data = new AdvertiseData.Builder()
                .SetIncludeDeviceName(true)
                .SetIncludeTxPowerLevel(true);

            if (adData != null)
            {
                if (adData.ManufacturerData != null)
                    data.AddManufacturerData(adData.ManufacturerData.CompanyId, adData.ManufacturerData.Data);

                foreach (var serviceUuid in adData.ServiceUuids)
                    data.AddServiceUuid(serviceUuid.ToParcelUuid());
            }

            this.context
                .Manager
                .Adapter
                .BluetoothLeAdvertiser
                .StartAdvertising(
                    settings.Build(),
                    data.Build(),
                    this.adCallbacks
                );

            return Task.CompletedTask;
        }


        public void StopAdvertising()
        {
            this.context
                .Manager
                .Adapter
                .BluetoothLeAdvertiser
                .StopAdvertising(this.adCallbacks);
            this.adCallbacks = null;
        }


        void Cleanup()
        {
            this.server?.Close();
            this.server = null;
        }
    }
}