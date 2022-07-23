using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using CoreBluetooth;
using Foundation;

namespace Shiny.BluetoothLE.Hosting;


public class BleHostingManager : IBleHostingManager
{
    readonly CBPeripheralManager manager = new();
    readonly Dictionary<string, GattService> services = new();


    async Task<ushort> PublishL2Cap(bool secure)
    {
        var tcs = new TaskCompletionSource<ushort>();

        var handler = new EventHandler<CBPeripheralManagerL2CapChannelOperationEventArgs>((sender, args) =>
        {
            if (args.Error == null)
            {
                tcs.TrySetResult(args.Psm);
            }
            else
            {
                tcs.TrySetException(new InvalidOperationException(args.Error.Description));
            }
        });
        this.manager.DidPublishL2CapChannel += handler;

        try
        {
            this.manager.PublishL2CapChannel(secure);
            return await tcs.Task.ConfigureAwait(false);
        }
        finally
        {
            this.manager.DidPublishL2CapChannel -= handler;
        }
    }


    public IObservable<L2CapChannel> WhenL2CapChannelOpened(bool secure) => Observable.Create<L2CapChannel>(async ob =>
    {
        var handler = new EventHandler<CBPeripheralManagerOpenL2CapChannelEventArgs>((sender, args) =>
        {
            ob.OnNext(new L2CapChannel(
                args.Channel!.Psm,
                args.Channel!.InputStream.ToStream(),
                args.Channel!.OutputStream.ToStream()
            ));
        });
        this.manager.DidOpenL2CapChannel += handler;
        var psm = await this.PublishL2Cap(secure);

        return () =>
        {
            this.manager.DidOpenL2CapChannel += handler;
            this.manager.UnpublishL2CapChannel(psm);
        };
    });


    // TODO
    public Task<AccessState> RequestAccess() => Task.FromResult(AccessState.Available);


    public AccessState Status => this.manager.State switch
    {
        CBManagerState.PoweredOff => AccessState.Disabled,
        CBManagerState.Unauthorized => AccessState.Denied,
        CBManagerState.Unsupported => AccessState.NotSupported,
        CBManagerState.PoweredOn => AccessState.Available,
        //  CBPeripheralManagerState.Resetting, Unknown
        _ => AccessState.Unknown
    };


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
            var serviceUuids = options.UseGattServiceUuids
                ? this.services.Keys.ToList()
                : options.ServiceUuids;

            //opts.LocalName 
            if (serviceUuids.Count > 0)
            {
                opts.ServicesUUID = serviceUuids
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
        // TODO: not sure about this
        //if (AppleExtensions.HasPlistValue("NSBluetoothAlwaysUsageDescription", 13) && !primary)
        //    throw new InvalidOperationException("You must specify your service as primary when using bg BLE");
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