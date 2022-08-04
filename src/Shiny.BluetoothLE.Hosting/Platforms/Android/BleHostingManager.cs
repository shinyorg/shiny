using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Android.Bluetooth;
using Android.Bluetooth.LE;
using Android.OS;
using Java.Util;
using Shiny.BluetoothLE.Hosting.Internals;
using static Android.Manifest;


namespace Shiny.BluetoothLE.Hosting
{
    public class BleHostingManager : IBleHostingManager
    {
        readonly GattServerContext context;
        readonly IPlatform platform;
        readonly Dictionary<string, GattService> services;
        AdvertisementCallbacks? adCallbacks;


        public BleHostingManager(IPlatform platform)
        {
            this.context = new GattServerContext(platform);
            this.services = new Dictionary<string, GattService>();
            this.platform = platform;
        }


        public async Task<AccessState> RequestAccess(bool advertise = true, bool connect = true)
        {
            if (!advertise && !connect)
                throw new ArgumentException("You must request at least 1 permission");

            if (!this.platform.IsMinApiLevel(23))
                return AccessState.NotSupported; //throw new InvalidOperationException("BLE Advertiser needs API Level 23+");

            var current = this.context.Manager.GetAccessState();
            if (current != AccessState.Available && current != AccessState.Unknown)
                return current;

            if (this.platform.IsMinApiLevel(31))
            {
                var perms = new List<string>();
                if (advertise)
                    perms.Add(Permission.BluetoothAdvertise);

                if (connect)
                    perms.Add(Permission.BluetoothConnect);

                var result = await this.platform.RequestPermissions(perms.ToArray());
                if (!result.IsSuccess())
                    return AccessState.Denied;
            }
            return AccessState.Available;
        }


        public bool IsAdvertising => this.adCallbacks != null;
        public IReadOnlyList<IGattService> Services => this.services.Values.Cast<IGattService>().ToArray();


        public async Task<IGattService> AddService(string uuid, bool primary, Action<IGattServiceBuilder> serviceBuilder)
        {
            (await this.RequestAccess(false, true)).Assert();

            var service = new GattService(this.context, uuid, primary);
            serviceBuilder(service);

            //var task = this.context
            //    .WhenServiceAdded
            //    .Take(1)
            //    .Timeout(TimeSpan.FromSeconds(5))
            //    .ToTask();

            if (!this.context.Server.AddService(service.Native))
                throw new InvalidOperationException("Service operation did not complete - look at logs");

            //await task.ConfigureAwait(false);
            this.services.Add(uuid, service);
            return service;
        }


        public void ClearServices()
        {
            this.services.Clear();
            this.context.Server.ClearServices();
            this.Cleanup();
        }


        public void RemoveService(string serviceUuid)
        {
            var uuid = UUID.FromString(serviceUuid);
            var s = this.services.ContainsKey(serviceUuid)
                ? this.services[serviceUuid].Native
                : this.context.Server.Services?.FirstOrDefault(x => x.Uuid?.Equals(uuid) ?? false);

            if (s != null)
                this.context.Server.RemoveService(s);

            this.services.Remove(serviceUuid);
            if (this.services.Count == 0)
                this.Cleanup();
        }


        public async Task StartAdvertising(AdvertisementOptions? options = null)
        {
            (await this.RequestAccess(true, false)).Assert();

            options ??= new AdvertisementOptions();
            var tcs = new TaskCompletionSource<bool>();
            this.adCallbacks = new AdvertisementCallbacks(
                () => tcs.SetResult(true),
                ex => tcs.SetException(ex)
            );

            var settings = new AdvertiseSettings.Builder()!
                .SetAdvertiseMode(AdvertiseMode.Balanced)!
                .SetConnectable(true);

            var data = new AdvertiseData.Builder();
            //var data = new AdvertiseData.Builder()
            //    .SetIncludeDeviceName(options.AndroidIncludeDeviceName)
            //    .SetIncludeTxPowerLevel(options.AndroidIncludeTxPower);

            //if (options.ManufacturerData != null)
            //    data = data.AddManufacturerData(options.ManufacturerData.CompanyId, options.ManufacturerData.Data);

            if (options.LocalName != null)
                BluetoothAdapter.DefaultAdapter.SetName(options.LocalName); // TODO: verify name length with exception

            foreach (var uuid in options.ServiceUuids)
            {
                var nativeUuid = UUID.FromString(uuid);
                var parcel = new ParcelUuid(nativeUuid);
                data = data.AddServiceUuid(parcel);
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

            this.services.Clear();
            this.context.CloseServer();
        }
    }
}