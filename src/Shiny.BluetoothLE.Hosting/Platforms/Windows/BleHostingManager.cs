using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Reactive.Linq;
using Windows.Storage.Streams;
using Windows.Devices.Bluetooth.Advertisement;
using Windows.Devices.Enumeration;
using Windows.Devices.Bluetooth;

namespace Shiny.BluetoothLE.Hosting
{
    public class BleHostingManager : IBleHostingManager
    {
        readonly BluetoothLEAdvertisementPublisher publisher = new BluetoothLEAdvertisementPublisher();
        readonly Dictionary<string, GattService> services = new Dictionary<string, GattService>();


        public async Task<AccessState> RequestAccess(bool advertise = true, bool gattConnect = true)
        {
            var adapter = await BluetoothAdapter.GetDefaultAsync();
            if (adapter == null)
                return AccessState.NotSupported;

            if (!adapter.IsLowEnergySupported || !adapter.IsPeripheralRoleSupported)
                return AccessState.NotSupported;

            return AccessState.Available;
        }


        public bool IsAdvertising => this.publisher.Status == BluetoothLEAdvertisementPublisherStatus.Started;

        public IReadOnlyList<IGattService> Services => this.services.Values.Cast<IGattService>().ToList();
        public IObservable<AccessState> WhenStatusChanged() => Observable.Return(AccessState.Available);
        public async Task<IGattService> AddService(string uuid, bool primary, Action<IGattServiceBuilder> serviceBuilder)
        {
            var sb = new GattService(uuid);
            serviceBuilder(sb);
            await sb.Build();
            this.services.Add(uuid, sb);

            return sb;
        }


        public void ClearServices()
        {
            foreach (var service in this.services.Values)
                service.Dispose();

            this.services.Clear();
        }


        public void RemoveService(string serviceUuid)
        {
            if (!this.services.ContainsKey(serviceUuid))
                return;

            this.services[serviceUuid].Dispose();
            this.services.Remove(serviceUuid);
        }


        public Task StartAdvertising(AdvertisementOptions? options = null)
        {
            options ??= new AdvertisementOptions();
            if (!options.LocalName.IsEmpty())
                this.publisher.Advertisement.LocalName = options.LocalName;

            this.publisher.Advertisement.Flags = BluetoothLEAdvertisementFlags.ClassicNotSupported;
            this.publisher.Advertisement.ManufacturerData.Clear();
            this.publisher.Advertisement.ServiceUuids.Clear();

            foreach (var serviceUuid in options.ServiceUuids)
            {
                var uuid = Utils.ToUuidType(serviceUuid);
                this.publisher.Advertisement.ServiceUuids.Add(uuid);
            }
            this.publisher.Start();
            return Task.CompletedTask;
        }


        public void StopAdvertising() => this.publisher.Stop();
    }
}