using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using CoreBluetooth;
using Foundation;


namespace Shiny.BluetoothLE.Hosting
{
    public class BleHostingManager : IBleHostingManager
    {
        readonly CBPeripheralManager manager = new CBPeripheralManager();
        readonly IDictionary<string, GattService> services = new Dictionary<string, GattService>();


        public Task<AccessState> RequestAccess(bool advertise = true, bool connect = true)
                => Task.FromResult(this.Status);



        public bool IsAdvertising => this.manager.Advertising;
        public async Task StartAdvertising(AdvertisementOptions? options = null)
        {
            if (this.manager.Advertising)
                throw new InvalidOperationException("Advertising is already active");

            if (this.Status != AccessState.Unknown && this.Status != AccessState.Available)
                throw new InvalidOperationException("Invalid Status: " + this.Status);

            options ??= new AdvertisementOptions();
            await this.manager
                .WhenReady()
                .Timeout(TimeSpan.FromSeconds(10))
                .ToTask();

            var tcs = new TaskCompletionSource<bool>();
            var handler = new EventHandler<NSErrorEventArgs>((sender, args) =>
            {
                if (args.Error == null)
                    tcs.SetResult(true);
                else
                    tcs.SetException(new ArgumentException(args.Error.LocalizedDescription));
            });

            try
            {
                this.manager.AdvertisingStarted += handler;

                var opts = new StartAdvertisingOptions();
                if (options.LocalName != null)
                    opts.LocalName = options.LocalName;

                if (options.ServiceUuids.Count > 0)
                {
                    opts.ServicesUUID = options
                        .ServiceUuids
                        .Select(CBUUID.FromString)
                        .ToArray();
                }

                this.manager.StartAdvertising(opts);
                await tcs.Task.ConfigureAwait(false);
            }
            finally
            {
                this.manager.AdvertisingStarted -= handler;
            }
        }


        public void StopAdvertising() => this.manager.StopAdvertising();


        public async Task<IGattService> AddService(string uuid, bool primary, Action<IGattServiceBuilder> serviceBuilder)
        {
            await this.manager
                .WhenReady()
                .Timeout(TimeSpan.FromSeconds(10))
                .ToTask();

            var service = new GattService(this.manager, uuid, primary);
            serviceBuilder(service);

            var tcs = new TaskCompletionSource<bool>();
            var handler = new EventHandler<CBPeripheralManagerServiceEventArgs>((sender, args) =>
            {
                if (args.Service.UUID != service.Native.UUID)
                    return;

                if (args.Error == null)
                    tcs.TrySetResult(true);
                else
                    tcs.SetException(new InvalidOperationException("Could not add BLE service - " + args.Error));
            });

            try
            {
                this.manager.ServiceAdded += handler;
                this.manager.AddService(service.Native);
                await tcs.Task.ConfigureAwait(false);

                this.services.Add(uuid, service);
            }
            finally
            {
                this.manager.ServiceAdded -= handler;
            }
            return service;
        }


        public void RemoveService(string serviceUuid)
        {
            if (!this.services.ContainsKey(serviceUuid))
            {
                var native = new CBMutableService(CBUUID.FromString(serviceUuid), false);
                this.manager.RemoveService(native); // let's try to remove anyhow
            }
            else
            {
                var service = this.services[serviceUuid];
                this.manager.RemoveService(service.Native);
                service.Dispose();
                this.services.Remove(serviceUuid);
            }
        }


        public void ClearServices()
        {
            foreach (var service in this.services.Values)
            {
                this.manager.RemoveService(service.Native);
                service.Dispose();
            }
            this.services.Clear();
            this.manager.RemoveAllServices();
        }


        public IReadOnlyList<IGattService> Services => this.services.Values.Cast<IGattService>().ToList();


        AccessState Status => this.manager.State switch
        {
            CBPeripheralManagerState.PoweredOff => AccessState.Disabled,
            CBPeripheralManagerState.Unauthorized => AccessState.Denied,
            CBPeripheralManagerState.Unsupported => AccessState.NotSupported,
            CBPeripheralManagerState.PoweredOn => AccessState.Available,
            //  CBPeripheralManagerState.Resetting, Unknown
            _ => AccessState.Unknown
        };
    }
}