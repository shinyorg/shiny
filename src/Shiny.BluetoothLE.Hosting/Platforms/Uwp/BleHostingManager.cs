using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Reactive.Linq;
using Windows.Storage.Streams;
using Windows.Devices.Bluetooth.Advertisement;


namespace Shiny.BluetoothLE.Hosting
{
    public class BleHostingManager : IBleHostingManager
    {
        readonly BluetoothLEAdvertisementPublisher publisher = new BluetoothLEAdvertisementPublisher();
        readonly Dictionary<string, GattService> services = new Dictionary<string, GattService>();


        public AccessState Status
        {
            get
            {
                return AccessState.Available;
                //var devices = await DeviceInformation.FindAllAsync(BluetoothAdapter.GetDeviceSelector());
                //foreach (var dev in devices)
                //{
                //    Log.Info(BleLogCategory.Adapter, "found - {dev.Name} ({dev.Kind} - {dev.Id})");

                //    var native = await BluetoothAdapter.FromIdAsync(dev.Id);
                //    if (native.IsLowEnergySupported)
                //    {
                //        var radio = await native.GetRadioAsync();
                //        var adapter = new Adapter(native, radio);
                //        ob.OnNext(adapter);
                //    }
                //}
                //if (this.native == null || !this.native.IsLowEnergySupported)
                //    return AdapterFeatures.None;

                //var features = AdapterFeatures.AllClient;
                //if (!this.native.IsCentralRoleSupported)
                //    features &= ~AdapterFeatures.AllClient;

                //if (this.native.IsPeripheralRoleSupported)
                //    features |= AdapterFeatures.AllServer;

                //if (this.native.IsCentralRoleSupported || this.native.IsPeripheralRoleSupported)
                //    features |= AdapterFeatures.AllControls;

                //return features;
            }
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

            if (options.ManufacturerData != null)
            {
                using (var writer = new DataWriter())
                {
                    writer.WriteBytes(options.ManufacturerData.Data);
                    var md = new BluetoothLEManufacturerData(options.ManufacturerData.CompanyId, writer.DetachBuffer());
                    this.publisher.Advertisement.ManufacturerData.Add(md);
                }
            }
            var serviceUuids = options.UseGattServiceUuids
                ? this.services.Keys.ToList()
                : options.ServiceUuids;

            foreach (var serviceUuid in serviceUuids)
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