using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Android.Bluetooth;
using Android.Bluetooth.LE;
using Java.Util;
using Shiny.BluetoothLE.Hosting.Internals;


namespace Shiny.BluetoothLE.Hosting
{
    public class BleHostingManager : IBleHostingManager
    {
        readonly GattServerContext context;
        readonly Dictionary<string, GattService> services;
        readonly IMessageBus messageBus;
        AdvertisementCallbacks? adCallbacks;


        public BleHostingManager(IAndroidContext context, IMessageBus messageBus)
        {
            this.context = new GattServerContext(context);
            this.services = new Dictionary<string, GattService>();
            this.messageBus = messageBus;
        }


        public AccessState Status => this.context.Manager.GetAccessState();
        public IObservable<AccessState> WhenStatusChanged() => this.messageBus
            .Listener<State>()
            .Select(x => x.FromNative())
            .StartWith(this.Status);
        public bool IsAdvertising => this.adCallbacks != null;
        public IReadOnlyList<IGattService> Services => this.services.Values.Cast<IGattService>().ToArray();


        public Task<IGattService> AddService(string uuid, bool primary, Action<IGattServiceBuilder> serviceBuilder)
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


        public void RemoveService(string serviceUuid)
        {
            var s = this.services[serviceUuid];
            this.context.Server.RemoveService(s.Native);
            this.services.Remove(serviceUuid);
            if (this.services.Count == 0)
                this.Cleanup();
        }


        public async Task StartAdvertising(AdvertisementOptions? options = null)
        {
            if (!this.context.Context.IsMinApiLevel(23))
                throw new ApplicationException("BLE Advertiser needs API Level 23+");

            options ??= new AdvertisementOptions();
            var tcs = new TaskCompletionSource<object>();
            this.adCallbacks = new AdvertisementCallbacks(
                () => tcs.SetResult(null),
                ex => tcs.SetException(ex)
            );

            var settings = new AdvertiseSettings.Builder()
                .SetAdvertiseMode(AdvertiseMode.Balanced)
                .SetConnectable(true);

            var data = new AdvertiseData.Builder()
                .SetIncludeDeviceName(options.AndroidIncludeDeviceName)
                .SetIncludeTxPowerLevel(options.AndroidIncludeTxPower);

            if (options.ManufacturerData != null)
                data.AddManufacturerData(options.ManufacturerData.CompanyId, options.ManufacturerData.Data);

            var serviceUuids = options.UseGattServiceUuids
                ? this.services.Keys.ToList()
                : options.ServiceUuids;

            foreach (var uuid in serviceUuids)
                data.AddServiceUuid(new Android.OS.ParcelUuid(UUID.FromString(uuid)));

            this.context
                .Manager
                .Adapter
                .BluetoothLeAdvertiser
                .StartAdvertising(
                    settings.Build(),
                    data.Build(),
                    this.adCallbacks
                );

            await tcs.Task;
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