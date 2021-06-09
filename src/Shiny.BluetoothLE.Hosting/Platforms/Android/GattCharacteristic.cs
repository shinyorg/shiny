using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Android.Bluetooth;
using Java.Util;
using Shiny.BluetoothLE.Hosting.Internals;


namespace Shiny.BluetoothLE.Hosting
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


        public GattCharacteristic(GattServerContext context, string uuid)
        {
            this.subscribers = new Dictionary<string, IPeripheral>();
            this.disposer = new CompositeDisposable();
            this.context = context;
            this.Uuid = uuid;
        }


        public BluetoothGattCharacteristic Native { get; private set; }
        public string Uuid { get; }
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
                this.properties |= GattProperty.Indicate;

            if (options.HasFlag(NotificationOptions.Notify))
                this.properties |= GattProperty.Notify;

            return this;
        }


        public IGattCharacteristicBuilder SetWrite(Func<WriteRequest, GattState> onWrite, WriteOptions options = WriteOptions.Write)
        {
            this.onWrite = onWrite;
            if (options.HasFlag(WriteOptions.EncryptionRequired))
            {
                this.permissions = GattPermission.WriteEncrypted;
            }
            else if (options.HasFlag(WriteOptions.AuthenticatedSignedWrites))
            {
                this.properties |= GattProperty.SignedWrite;
                this.permissions |= GattPermission.WriteSigned;
            }
            else
            {
                this.properties |= GattProperty.Write;
                this.permissions |= GattPermission.Write;
            }
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
            else
                this.permissions |= GattPermission.Read;

            return this;
        }


        public void Build()
        {
            var nativeUuid = Utils.ToUuidType(this.Uuid);

            this.Native = new BluetoothGattCharacteristic(
                nativeUuid,
                this.properties,
                this.permissions
            );

            this.SetupNotifications();
            this.SetupRead();
            this.SetupWrite();
        }


        public void Dispose() => this.disposer.Dispose();


        void SetupNotifications()
        {
            if (this.onSubscribe == null)
                return;

            var ndesc = new BluetoothGattDescriptor(
                Constants.NotifyDescriptorUuid,
                GattDescriptorPermission.Read | GattDescriptorPermission.Write
            );
            this.Native.AddDescriptor(ndesc);

            this.context
                .DescriptorWrite
                .Where(x => x.Descriptor.Equals(ndesc))
                .Subscribe(x =>
                {
                    var respond = true;
                    if (x.Value.SequenceEqual(Constants.IndicateEnableBytes) || x.Value.SequenceEqual(Constants.NotifyEnableBytes))
                    {
                        var peripheral = this.GetOrAdd(x.Device);
                        this.onSubscribe(new CharacteristicSubscription(this, peripheral, true));
                    }
                    else if (x.Value.SequenceEqual(Constants.NotifyDisableBytes))
                    {
                        var peripheral = this.Remove(x.Device);
                        if (peripheral != null)
                            this.onSubscribe(new CharacteristicSubscription(this, peripheral, false));
                    }
                    else
                        respond = false;

                    if (respond && x.ResponseNeeded)
                    {
                        this.context.Server.SendResponse(
                            x.Device,
                            x.RequestId,
                            GattStatus.Success,
                            0,
                            new byte[] { 0x1 }
                        );
                    }
                })
                .DisposedBy(this.disposer);

            this.context
                .ConnectionStateChanged
                .Where(x => x.NewState == ProfileState.Disconnected)
                .Subscribe(x =>
                {
                    var peripheral = this.Remove(x.Device);
                    if (peripheral != null)
                        this.onSubscribe(new CharacteristicSubscription(this, peripheral, false));
                })
                .DisposedBy(this.disposer);
        }


        void SetupRead()
        {
            if (this.onRead == null)
                return;

            this.context
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


        void SetupWrite()
        {
            if (this.onWrite == null)
                return;

            this.context
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

