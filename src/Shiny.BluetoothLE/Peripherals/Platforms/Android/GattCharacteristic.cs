using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Android.Bluetooth;
using Shiny.BluetoothLE.Peripherals.Internals;


namespace Shiny.BluetoothLE.Peripherals
{
    public class GattCharacteristic : IGattCharacteristic, IGattCharacteristicBuilder
    {
        readonly GattServerContext context;
        GattProperty properties = 0;
        GattPermission permissions = 0;


        public GattCharacteristic(GattServerContext context, Guid uuid)
        {
            this.context = context;
            this.Uuid = uuid;
        }


        public BluetoothGattCharacteristic Native { get; private set; }
        public Guid Uuid { get; }
        public CharacteristicProperties Properties { get; }
        public IReadOnlyList<IPeripheral> SubscribedCentrals => throw new NotImplementedException();

        public Task Notify(byte[] data, params IPeripheral[] centrals)
        {

            throw new NotImplementedException();
        }

        public IGattCharacteristicBuilder SetNotification(Action<CharacteristicSubscription> onSubscribe = null, NotificationOptions options = NotificationOptions.Notify)
        {
            throw new NotImplementedException();
        }

        public IGattCharacteristicBuilder SetWrite(Func<WriteRequest, GattState> request, WriteOptions options = WriteOptions.Write)
        {
            throw new NotImplementedException();
        }

        public IGattCharacteristicBuilder SetRead(Func<ReadRequest, ReadResult> request, bool encrypted = false)
        {
            throw new NotImplementedException();
        }

        public void Build()
        {
            this.Native = new BluetoothGattCharacteristic(
                this.Uuid.ToUuid(),
                this.properties,
                this.permissions
            );
        }
    }
}

//            this.subscribers = new Dictionary<string, IPeripheral>();
//            this.Native = new BluetoothGattCharacteristic(
//                uuid.ToUuid(),
//                properties.ToNative(),
//                permissions.ToNative()
//            );


//        public override IReadOnlyList<IPeripheral> SubscribedDevices
//        {
//            get
//            {
//                lock (this.subscribers)
//                {
//                    return new ReadOnlyCollection<IPeripheral>(this.subscribers.Values.ToArray());
//                }
//            }
//        }


//        public override void Broadcast(byte[] value, params IPeripheral[] peripherals)
//        {
//            this.Native.SetValue(value);

//            if (peripherals == null || peripherals.Length == 0)
//                peripherals = this.subscribers.Values.ToArray();

//            foreach (var x in peripherals.OfType<Device>())
//            {
//                lock (this.context.ServerReadWriteLock)
//                {
//                    this.context.Server.NotifyCharacteristicChanged(x.Native, this.Native, false);
//                }
//            }
//        }


//        public override IObservable<CharacteristicBroadcast> BroadcastObserve(byte[] value, params IPeripheral[] peripherals)
//            => Observable.Create<CharacteristicBroadcast>(ob =>
//            {
//                var cancel = false;
//                this.Native.SetValue(value);

//                if (peripherals == null || peripherals.Length == 0)
//                    peripherals = this.subscribers.Values.ToArray();

//                var indicate = this.Properties.HasFlag(CharacteristicProperties.Indicate);
//                foreach (var x in peripherals.OfType<Device>())
//                {
//                    if (!cancel)
//                    {
//                        lock (this.context.ServerReadWriteLock)
//                        {
//                            if (!cancel)
//                            {
//                                var result = this.context.Server.NotifyCharacteristicChanged(x.Native, this.Native, indicate);
//                                ob.OnNext(new CharacteristicBroadcast(x, this, value, indicate, result));
//                            }
//                        }
//                    }
//                }

//                ob.OnCompleted();
//                return () => cancel = true;
//            });


//        IObservable<DeviceSubscriptionEvent> subscriptionOb;
//        public override IObservable<DeviceSubscriptionEvent> WhenDeviceSubscriptionChanged()
//        {
//            this.subscriptionOb = this.subscriptionOb ?? Observable.Create<DeviceSubscriptionEvent>(ob =>
//            {
//                var handler = new EventHandler<DescriptorWriteEventArgs>((sender, args) =>
//                {
//                    if (args.Descriptor.Equals(this.NotificationDescriptor))
//                    {
//                        if (args.Value.SequenceEqual(NotifyEnabledBytes) || args.Value.SequenceEqual(IndicateEnableBytes))
//                        {
//                            var device = this.GetOrAdd(args.Device);
//                            ob.OnNext(new DeviceSubscriptionEvent(device, true));
//                        }
//                        else
//                        {
//                            var device = this.Remove(args.Device);
//                            if (device != null)
//                                ob.OnNext(new DeviceSubscriptionEvent(device, false));
//                        }
//                    }
//                });
//                var dhandler = new EventHandler<ConnectionStateChangeEventArgs>((sender, args) =>
//                {
//                    if (args.NewState != ProfileState.Disconnected)
//                        return;

//                    var device = this.Remove(args.Device);
//                    if (device != null)
//                        ob.OnNext(new DeviceSubscriptionEvent(device, false));
//                });

//                this.context.Callbacks.ConnectionStateChanged += dhandler;
//                this.context.Callbacks.DescriptorWrite += handler;

//                return () =>
//                {
//                    this.context.Callbacks.DescriptorWrite -= handler;
//                    this.context.Callbacks.ConnectionStateChanged -= dhandler;
//                };
//            })
//            .Publish()
//            .RefCount();

//            return this.subscriptionOb;
//        }


//        public override IObservable<WriteRequest> WhenWriteReceived() => Observable.Create<WriteRequest>(ob =>
//        {
//            var handler = new EventHandler<CharacteristicWriteEventArgs>((sender, args) =>
//            {
//                if (!args.Characteristic.Equals(this.Native))
//                    return;

//                var device = new Device(args.Device);
//                var request = new WriteRequest(device, args.Value, args.Offset, args.ResponseNeeded);
//                ob.OnNext(request);

//                if (request.IsReplyNeeded)
//                {
//                    lock (this.context.ServerReadWriteLock)
//                    {
//                        this.context.Server.SendResponse
//                        (
//                            args.Device,
//                            args.RequestId,
//                            request.Status.ToNative(),
//                            request.Offset,
//                            request.Value
//                        );
//                    }
//                }
//            });
//            this.context.Callbacks.CharacteristicWrite += handler;

//            return () => this.context.Callbacks.CharacteristicWrite -= handler;
//        });


//        public override IObservable<ReadRequest> WhenReadReceived() => Observable.Create<ReadRequest>(ob =>
//        {
//            var handler = new EventHandler<CharacteristicReadEventArgs>((sender, args) =>
//            {
//                if (!args.Characteristic.Equals(this.Native))
//                    return;

//                var device = new Device(args.Device);
//                var request = new ReadRequest(device, args.Offset);
//                ob.OnNext(request);

//                lock (this.context.ServerReadWriteLock)
//                {
//                    this.context.Server.SendResponse(
//                        args.Device,
//                        args.RequestId,
//                        request.Status.ToNative(),
//                        args.Offset,
//                        request.Value
//                    );
//                }
//            });
//            this.context.Callbacks.CharacteristicRead += handler;
//            return () => this.context.Callbacks.CharacteristicRead -= handler;
//        });


//        protected override IGattDescriptor CreateNative(Guid uuid, byte[] value)
//        {
//            var desc = new GattDescriptor(this, uuid, value);
//            this.Native.AddDescriptor(desc.Native);
//            return desc;
//        }


//        IPeripheral GetOrAdd(BluetoothDevice native)
//        {
//            lock (this.subscribers)
//            {
//                if (this.subscribers.ContainsKey(native.Address))
//                    return this.subscribers[native.Address];

//                var device = new Device(native);
//                this.subscribers.Add(native.Address, device);
//                return device;
//            }
//        }


//        IPeripheral Remove(BluetoothDevice native)
//        {
//            lock (this.subscribers)
//            {
//                if (this.subscribers.ContainsKey(native.Address))
//                {
//                    var device = this.subscribers[native.Address];
//                    this.subscribers.Remove(native.Address);
//                    return device;
//                }
//                return null;
//            }
//        }




        //    readonly GattServerContext context;
        //readonly Guid uuid;
        //GattProperty properties = 0;
        //GattPermission permissions = 0;


        //public GattCharacteristicBuilder(GattServerContext context, Guid uuid)
        //{
        //    this.context = context;
        //    this.uuid = uuid;
        //}



        //public override IGattCharacteristicBuilder SetNotification(Action<CharacteristicSubscription> onSubscribe = null, NotificationOptions options = NotificationOptions.Notify)
        //{
        //    if (options.HasFlag(NotificationOptions.Indicate))
        //        this.properties |= GattProperty.Indicate;

        //    if (options.HasFlag(NotificationOptions.Notify))
        //        this.properties |= GattProperty.Notify;

        //    return base.SetNotification(onSubscribe, options);
        //}


        //public override IGattCharacteristicBuilder SetRead(Func<ReadRequest, ReadResult> request, bool encrypted = false)
        //{
        //    this.permissions |= encrypted
        //        ? GattPermission.ReadEncrypted
        //        : GattPermission.Read;

        //    this.properties |= GattProperty.Read;

        //    return base.SetRead(request, encrypted);
        //}


        //public override IGattCharacteristicBuilder SetWrite(Func<WriteRequest, GattState> request, WriteOptions options = WriteOptions.Write)
        //{
        //    this.permissions |= options.HasFlag(WriteOptions.EncryptionRequired)
        //        ? GattPermission.WriteEncrypted
        //        : GattPermission.Write;

        //    if (options.HasFlag(WriteOptions.Write))
        //        this.properties |= GattProperty.Write;

        //    if (options.HasFlag(WriteOptions.WriteWithoutResponse))
        //        this.properties |= GattProperty.WriteNoResponse;

        //    if (options.HasFlag(WriteOptions.AuthenticatedSignedWrites))
        //        this.properties |= GattProperty.SignedWrite;

        //    return base.SetWrite(request, options);
        //}


        //public GattCharacteristic Build()
        //{
        //    var native = new BluetoothGattCharacteristic(
        //        this.uuid.ToUuid(),
        //        this.properties,
        //        this.permissions
        //    );

        //    if (this.properties.HasFlag(GattProperty.Notify) || this.properties.HasFlag(GattProperty.Indicate))
        //    {
        //        var ndesc = new BluetoothGattDescriptor(
        //            Constants.NotifyDescriptorUuid,
        //            GattDescriptorPermission.Read | GattDescriptorPermission.Write
        //        );
        //        native.AddDescriptor(ndesc);
        //    }
        //    return new GattCharacteristic(this.context, native);
        //}