using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Shiny.BluetoothLE.Hosting.Internals;
using Android.Bluetooth;
using Android.Bluetooth.LE;


namespace Shiny.BluetoothLE.Hosting
{
    public class PeripheralManager : IBleHostingManager
    {
        readonly GattServerContext context;
        readonly Dictionary<Guid, GattService> services;
        readonly IMessageBus messageBus;
        AdvertisementCallbacks? adCallbacks;


        public PeripheralManager(AndroidContext context, IMessageBus messageBus)
        {
            this.context = new GattServerContext(context);
            this.services = new Dictionary<Guid, GattService>();
            this.messageBus = messageBus;
        }


        public AccessState Status => this.context.Manager.GetAccessState();
        public IObservable<AccessState> WhenStatusChanged() => this.messageBus
            .Listener<State>()
            .Select(x => x.FromNative())
            .StartWith(this.Status);
        public bool IsAdvertising => this.adCallbacks != null;
        public IReadOnlyList<IGattService> Services => this.services.Values.Cast<IGattService>().ToArray();


        public Task<IGattService> AddService(Guid uuid, bool primary, Action<IGattServiceBuilder> serviceBuilder)
        {
            var service = new GattService(this.context, uuid, primary);
            serviceBuilder(service);
            this.context.Server.AddService(service.Native);
            return Task.FromResult<IGattService>(service);
        }


        public void ClearServices()
        {
            this.services.Clear();
            this.context.Server.ClearServices();
            this.Cleanup();
        }


        public void RemoveService(Guid serviceUuid)
        {
            var s = this.services[serviceUuid];
            this.context.Server.RemoveService(s.Native);
            this.services.Remove(serviceUuid);
            if (this.services.Count == 0)
                this.Cleanup();
        }


        public Task StartAdvertising(AdvertisementData? adData = null)
        {
            if (!this.context.Context.IsMinApiLevel(23))
                throw new ApplicationException("BLE Advertiser needs API Level 23+");

            this.adCallbacks = new AdvertisementCallbacks();

            var settings = new AdvertiseSettings.Builder()
                .SetAdvertiseMode(AdvertiseMode.Balanced)
                .SetConnectable(true); // TODO: configurable

            var data = new AdvertiseData.Builder()
                .SetIncludeDeviceName(true) // TODO: configurable
                .SetIncludeTxPowerLevel(true); // TODO: configurable

            if (adData != null)
            {
                if (adData.ManufacturerData != null)
                    data.AddManufacturerData(adData.ManufacturerData.CompanyId, adData.ManufacturerData.Data);

                if (adData.ServiceUuids != null)
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
            foreach (var service in this.services)
                service.Value.Dispose();

            this.context.CloseServer();
        }
    }
}