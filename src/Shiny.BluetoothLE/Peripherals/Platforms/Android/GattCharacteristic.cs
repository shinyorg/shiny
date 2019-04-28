using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Android.Bluetooth;
using Shiny.BluetoothLE.Peripherals.Internals;


namespace Shiny.BluetoothLE.Peripherals
{
    public class GattCharacteristic : IGattCharacteristic, IGattCharacteristicBuilder, IDisposable
    {
        readonly GattServerContext context;
        readonly CompositeDisposable disposer;
        readonly IDictionary<string, IPeripheral> subscribers;
        Action<CharacteristicSubscription> onSubscribe;
        Func<WriteRequest, GattState> onWrite;
        Func<ReadRequest, ReadResult> onRead;
        GattProperty properties = 0;
        GattPermission permissions = 0;


        public GattCharacteristic(GattServerContext context, Guid uuid)
        {
            this.subscribers = new Dictionary<string, IPeripheral>();
            this.disposer = new CompositeDisposable();
            this.context = context;
            this.Uuid = uuid;
        }


        public BluetoothGattCharacteristic Native { get; private set; }
        public Guid Uuid { get; }
        public CharacteristicProperties Properties { get; }
        public IReadOnlyList<IPeripheral> SubscribedCentrals
        {
            get
            {
                lock (this.subscribers)
                {
                    return this.subscribers.Values.ToList();
                }
            }
        }


        public Task Notify(byte[] data, params IPeripheral[] centrals)
        {
            this.Native.SetValue(data);
            var sendTo = (centrals.OfType<Peripheral>() ?? this.SubscribedCentrals.OfType<Peripheral>()).ToArray();

            foreach (var send in sendTo)
            {
                this.context.Server.NotifyCharacteristicChanged(send.Native, this.Native, false);
            }
            return Task.CompletedTask;
        }


        public IGattCharacteristicBuilder SetNotification(Action<CharacteristicSubscription> onSubscribe = null, NotificationOptions options = NotificationOptions.Notify)
        {
            this.onSubscribe = onSubscribe;
            if (options.HasFlag(NotificationOptions.Indicate))
                this.properties = GattProperty.Indicate;

            if (options.HasFlag(NotificationOptions.Notify))
                this.properties = GattProperty.Notify;

            return this;
        }


        public IGattCharacteristicBuilder SetWrite(Func<WriteRequest, GattState> onWrite, WriteOptions options = WriteOptions.Write)
        {
            this.onWrite = onWrite;
            if (options.HasFlag(NotificationOptions.EncryptionRequired))
                this.permissions = GattPermission.WriteEncrypted;

            if (options.HasFlag(WriteOptions.AuthenticatedSignedWrites))
                this.properties |= GattProperty.SignedWrite;

            if (options.HasFlag(WriteOptions.Write))
                this.properties |= GattProperty.Write;

            if (options.HasFlag(WriteOptions.WriteWithoutResponse))
                this.properties |= GattProperty.WriteNoResponse;

            return this;
        }


        public IGattCharacteristicBuilder SetRead(Func<ReadRequest, ReadResult> onRead, bool encrypted = false)
        {
            this.onRead = onRead;
            this.properties |= GattProperty.Read;
            if (encrypted)
                this.permissions |= GattPermission.ReadEncrypted;
            return this;
        }


        public void Build()
        {
            this.Native = new BluetoothGattCharacteristic(
                this.Uuid.ToUuid(),
                this.properties,
                this.permissions
            );
            if (this.onRead != null)
            {
                this.context
                    .Callbacks
                    .CharacteristicRead
                    .Where(x => x.Characteristic.Equals(this.Native))
                    .Subscribe(ch =>
                    {
                        var peripheral = new Peripheral(ch.Device);
                        var request = new ReadRequest(this, peripheral, ch.Offset);
                        var result = this.onRead(request);

                        this.context.Server.SendResponse
                        (
                            ch.Device,
                            ch.RequestId,
                            result.Status.ToNative(),
                            ch.Offset,
                            result.Data
                        );
                    })
                    .DisposedBy(this.disposer);
            }
            if (this.onSubscribe != null)
            {
                this.context
                    .Callbacks
                    .DescriptorWrite
                    .Subscribe(x =>
                    {

                    })
                    .DisposedBy(this.disposer);

                this.context
                    .Callbacks
                    .ConnectionStateChanged
                    .Where(x => x.NewState == ProfileState.Disconnected)
                    .Subscribe(x =>
                    {
                        var peripheral = this.Remove(x.Device);
                        if (peripheral != null)
                            this.onSubscribe(new CharacteristicSubscription(this, peripheral, false));
                    })
                    .DisposedBy(this.disposer);

                    //if (args.Descriptor.Equals(this.NotificationDescriptor))
                    //.Where(x => )

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
            }
            if (this.onWrite != null)
            {
                this.context
                    .Callbacks
                    .CharacteristicWrite
                    .Where(x => x.Characteristic.Equals(this.Native))
                    .Subscribe(ch =>
                    {
                        var peripheral = new Peripheral(ch.Device);
                        var request = new WriteRequest(this, peripheral, ch.Value, ch.Offset, ch.ResponseNeeded);
                        var state = this.onWrite(request);

                        if (request.IsReplyNeeded)
                        {
                            this.context.Server.SendResponse(
                                ch.Device,
                                ch.RequestId,
                                state.ToNative(),
                                ch.Offset,
                                ch.Value
                            );
                        }
                    })
                    .DisposedBy(this.disposer);
            }

            if (this.onSubscribe != null)
            {
                var ndesc = new BluetoothGattDescriptor(
                    Constants.NotifyDescriptorUuid,
                    GattDescriptorPermission.Read | GattDescriptorPermission.Write
                );
                this.Native.AddDescriptor(ndesc);
            }
        }


        public void Dispose() => this.disposer.Dispose();


        IPeripheral GetOrAdd(BluetoothDevice native)
        {
            lock (this.subscribers)
            {
                if (this.subscribers.ContainsKey(native.Address))
                    return this.subscribers[native.Address];

                var device = new Peripheral(native);
                this.subscribers.Add(native.Address, device);
                return device;
            }
        }


        IPeripheral Remove(BluetoothDevice native)
        {
            lock (this.subscribers)
            {
                if (this.subscribers.ContainsKey(native.Address))
                {
                    var device = this.subscribers[native.Address];
                    this.subscribers.Remove(native.Address);
                    return device;
                }
                return null;
            }
        }
    }
}

