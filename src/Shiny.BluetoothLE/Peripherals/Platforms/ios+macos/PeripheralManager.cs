﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using CoreBluetooth;


namespace Shiny.BluetoothLE.Peripherals
{
    public class PeripheralManager : IPeripheralManager
    {
        readonly CBPeripheralManager manager;
        readonly IDictionary<Guid, GattService> services;


        public PeripheralManager()
        {
            this.manager = new CBPeripheralManager();
            this.services = new Dictionary<Guid, GattService>();
        }


        public AccessState Status
        {
            get
            {
                switch (this.manager.State)
                {
                    case CBPeripheralManagerState.PoweredOff:
                        return AccessState.Disabled;

                    case CBPeripheralManagerState.Unauthorized:
                        return AccessState.Denied;

                    case CBPeripheralManagerState.Unsupported:
                        return AccessState.NotSupported;

                    case CBPeripheralManagerState.PoweredOn:
                        return AccessState.Available;

                    case CBPeripheralManagerState.Resetting:
                    case CBPeripheralManagerState.Unknown:
                    default:
                        return AccessState.Unknown;
                }
            }
        }


        public IObservable<AccessState> WhenStatusChanged() => Observable.Create<AccessState>(ob =>
        {
            var handler = new EventHandler((sender, args) => ob.Respond(this.Status));
            this.manager.StateUpdated += handler;
            return () => this.manager.StateUpdated -= handler;
        });


        public bool IsAdvertising => this.manager.Advertising;
        public async Task StartAdvertising(AdvertisementData? adData = null)
        {
            if (this.manager.Advertising)
                throw new ArgumentException("Advertising is already active");

            await this.manager.WhenReady().ToTask();
            this.manager.StartAdvertising(new StartAdvertisingOptions
            {
                LocalName = adData?.LocalName,
                ServicesUUID = this.services
                    .Select(x => x.Value.Uuid.ToCBUuid())
                    .ToArray()
            });
        }


        public void StopAdvertising() => this.manager.StopAdvertising();


        public Task<IGattService> AddService(Guid uuid, bool primary, Action<IGattServiceBuilder> serviceBuilder)
        {
            var service = new GattService(this.manager, uuid, primary);
            serviceBuilder(service);
            this.services.Add(uuid, service);
            this.manager.AddService(service.Native);
            return Task.FromResult<IGattService>(service);
        }


        public void RemoveService(Guid serviceUuid)
        {
            if (!this.services.ContainsKey(serviceUuid))
                return;

            var service = this.services[serviceUuid];
            this.manager.RemoveService(service.Native);
            service.Dispose();

            this.services.Remove(serviceUuid);
        }


        public void ClearServices()
        {
            //this.manager.RemoveAllServices();
            foreach (var service in this.services.Values)
            {
                this.manager.RemoveService(service.Native);
                service.Dispose();
            }
            this.services.Clear();
        }


        public IReadOnlyList<IGattService> Services => this.services.Values.Cast<IGattService>().ToList();
    }
}


//        public override void Broadcast(byte[] value, params IPeripheral[] peripherals)
//        {
//            var data = NSData.FromArray(value);
//            var devs = peripherals.OfType<Device>().ToList();
//            if (devs.Count == 0)
//            {
//                devs = this.SubscribedDevices.OfType<Device>().ToList();
//            }
//            this.manager.UpdateValue(data, this.Native, devs.Select(x => x.Central).ToArray());
//        }


//        public override IObservable<CharacteristicBroadcast> BroadcastObserve(byte[] value, params IPeripheral[] peripherals)
//            => Observable.Create<CharacteristicBroadcast>(ob =>
//            {
//                var data = NSData.FromArray(value);

//                var devs = peripherals.OfType<Device>().ToList();
//                if (devs.Count == 0)
//                {
//                    devs = this.SubscribedDevices.OfType<Device>().ToList();
//                }
//                this.manager.UpdateValue(data, this.Native, devs.Select(x => x.Central).ToArray());

//                var indicate = this.Properties.HasFlag(CharacteristicProperties.Indicate);
//                foreach (var dev in devs)
//                {
//                    ob.OnNext(new CharacteristicBroadcast(dev, this, value, indicate, true));
//                }
//                ob.OnCompleted();

//                return Disposable.Empty;
//            });


//        public override IObservable<WriteRequest> WhenWriteReceived()
//        {
//            this.writeOb = this.writeOb ?? Observable.Create<WriteRequest>(ob =>
//            {
//                var handler = new EventHandler<CBATTRequestsEventArgs>((sender, args) =>
//                {
//                    var writeWithResponse = this.Properties.HasFlag(CharacteristicProperties.Write);
//                    foreach (var native in args.Requests)
//                    {
//                        if (native.Characteristic.Equals(this.Native))
//                        {
//                            var device = new Device(native.Central);
//                            var request = new WriteRequest(device, native.Value.ToArray(), (int)native.Offset, false);
//                            ob.OnNext(request);

//                            if (writeWithResponse)
//                            {
//                                var status = (CBATTError) Enum.Parse(typeof(CBATTError), request.Status.ToString());
//                                this.manager.RespondToRequest(native, status);
//                            }
//                        }
//                    }
//                });
//                this.manager.WriteRequestsReceived += handler;
//                return () => this.manager.WriteRequestsReceived -= handler;
//            })
//            .Publish()
//            .RefCount();

//            return this.writeOb;
//        }



//        public override IObservable<ReadRequest> WhenReadReceived()
//        {
//            this.readOb = this.readOb ?? Observable.Create<ReadRequest>(ob =>
//            {
//                var handler = new EventHandler<CBATTRequestEventArgs>((sender, args) =>
//                {
//                    if (args.Request.Characteristic.Equals(this.Native))
//                    {
//                        var device = new Device(args.Request.Central);
//                        var request = new ReadRequest(device, (int)args.Request.Offset);
//                        ob.OnNext(request);

//                        var nativeStatus = (CBATTError) Enum.Parse(typeof(CBATTError), request.Status.ToString());
//                        args.Request.Value = NSData.FromArray(request.Value);
//                        this.manager.RespondToRequest(args.Request, nativeStatus);
//                    }
//                });
//                this.manager.ReadRequestReceived += handler;
//                return () => this.manager.ReadRequestReceived -= handler;
//            })
//            .Publish()
//            .RefCount();

//            return this.readOb;
//        }


//        protected override IGattDescriptor CreateNative(Guid uuid, byte[] value) => new GattDescriptor(this, uuid, value);


//        IPeripheral GetOrAdd(CBCentral central)
//        {
//            lock (this.subscribers)
//            {
//                if (this.subscribers.ContainsKey(central.Identifier))
//                    return this.subscribers[central.Identifier];

//                var device = new Device(central);
//                this.subscribers.Add(central.Identifier, device);
//                return device;
//            }
//        }


//        IPeripheral Remove(CBCentral central)
//        {
//            lock (this.subscribers)
//            {
//                if (this.subscribers.ContainsKey(central.Identifier))
//                {
//                    var device = this.subscribers[central.Identifier];
//                    this.subscribers.Remove(central.Identifier);
//                    return device;
//                }
//                return null;
//            }
//        }