using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth.GenericAttributeProfile;


namespace Shiny.BluetoothLE.Hosting
{
    public class GattCharacteristic : IGattCharacteristic, IGattCharacteristicBuilder, IDisposable
    {
        GattCharacteristicProperties properties = 0;
        GattProtectionLevel readProtection = GattProtectionLevel.Plain;
        GattProtectionLevel writeProtection = GattProtectionLevel.Plain;

        public GattCharacteristic(string uuid) => this.Uuid = uuid;


        public string Uuid { get; }
        public CharacteristicProperties Properties => throw new NotImplementedException();
        public IReadOnlyList<IPeripheral> SubscribedCentrals => throw new NotImplementedException();

        public Task Notify(byte[] data, params IPeripheral[] centrals)
        {
//            var buffer = value.AsBuffer();
//            this.native.NotifyValueAsync(buffer); // TODO: peripheral filtering
            throw new NotImplementedException();
        }


        public IGattCharacteristicBuilder SetNotification(Action<CharacteristicSubscription>? onSubscribe = null, NotificationOptions options = NotificationOptions.Notify)
        {
            return this;
        }


        public IGattCharacteristicBuilder SetRead(Func<ReadRequest, ReadResult> request, bool encrypted = false)
        {

            this.properties |= GattCharacteristicProperties.Read;

            return this;
        }


        public IGattCharacteristicBuilder SetWrite(Func<WriteRequest, GattState> request, WriteOptions options = WriteOptions.Write)
        {
            var enc = options.HasFlag(WriteOptions.EncryptionRequired);
            var auth = options.HasFlag(WriteOptions.AuthenticatedSignedWrites);

            if (enc && auth)
                this.writeProtection = GattProtectionLevel.EncryptionAndAuthenticationRequired;
            else if (enc)
                this.writeProtection = GattProtectionLevel.EncryptionRequired;
            else if (auth)
                this.writeProtection = GattProtectionLevel.AuthenticationRequired;

            if (options.HasFlag(WriteOptions.Write))
                this.properties |= GattCharacteristicProperties.Write;

            if (options.HasFlag(WriteOptions.WriteWithoutResponse))
                this.properties |= GattCharacteristicProperties.WriteWithoutResponse;

            return this;
        }


        public async Task Build(GattServiceProviderResult native)
        {
            var parameters = new GattLocalCharacteristicParameters
            {
                CharacteristicProperties = this.properties,
                ReadProtectionLevel = this.readProtection,
                WriteProtectionLevel = this.writeProtection
            };

            var uuid = Utils.ToUuidType(this.Uuid);
            var characteristic = await native
                .ServiceProvider
                .Service
                .CreateCharacteristicAsync(uuid, parameters);
        }

        public void Dispose()
        {
        }
    }
}



//                    using(args.GetDeferral())
//                    {
//                        var request = await args.GetRequestAsync();
//var dev = this.FindDevice(args.Session);
//var read = new ReadRequest(dev, (int)request.Length);
//ob.OnNext(read);

//                        if (read.Status == GattStatus.Success)
//                            request.RespondWithValue(read.Value.AsBuffer());
//                        else
//                            request.RespondWithProtocolError((byte) read.Status);
//                    }



//        public override IObservable<CharacteristicBroadcast> BroadcastObserve(byte[] value, params IPeripheral[] peripherals)
//        {
//            return Observable.Create<CharacteristicBroadcast>(async ob =>
//            {
//                var buffer = value.AsBuffer();

//                var result = await this.native.NotifyValueAsync(buffer); // TODO: get clients
//                //result[0].ProtocolError
//                ob.OnNext(new CharacteristicBroadcast(null, this, value, false, true)); // TODO: errors and such
//                ob.OnCompleted();

//                return Disposable.Empty;
//            });
//        }


//        IObservable<DeviceSubscriptionEvent> subscriptionOb;
//        public override IObservable<DeviceSubscriptionEvent> WhenDeviceSubscriptionChanged()
//        {
//            this.subscriptionOb = this.subscriptionOb ?? Observable.Create<DeviceSubscriptionEvent>(ob =>
//            {
//                var handler = new TypedEventHandler<GattLocalCharacteristic, object>((sender, args) =>
//                {
//                    // check for dropped subscriptions
//                    var copy = this.SubscribedDevices.ToList(); // copy
//                    foreach (var device in copy)
//                    {
//                        var found = sender.SubscribedClients.Any(x => x.Session.DeviceId.Id.Equals(device.Uuid.ToString()));
//                        if (!found)
//                        {
//                            this.connectedDevices.Remove(device);
//                            ob.OnNext(new DeviceSubscriptionEvent(device, false));
//                        }
//                    }
//                    foreach (var client in sender.SubscribedClients)
//                    {
//                        var dev = this.FindDevice(client.Session);
//                        if (dev != null)
//                        {
//                            ob.OnNext(new DeviceSubscriptionEvent(dev, false)); // now have to
//                            break;
//                        }
//                    }

//                    // check for new subscriptions
//                    foreach (var client in sender.SubscribedClients)
//                    {
//                        var dev = this.FindDevice(client.Session);
//                        if (dev == null)
//                        {
//                            dev = this.AddConnectedDevice(client.Session);
//                            ob.OnNext(new DeviceSubscriptionEvent(dev, true));
//                        }
//                    }
//                });
//                var sub = this.nativeReady.Subscribe(ch => this.native.SubscribedClientsChanged += handler);

//                return () =>
//                {
//                    sub.Dispose();
//                    if (this.native != null)
//                        this.native.SubscribedClientsChanged -= handler;
//                };
//            })
//            .Publish()
//            .RefCount();

//            return this.subscriptionOb;
//        }


//        IObservable<WriteRequest> writeOb;
//        public override IObservable<WriteRequest> WhenWriteReceived()
//        {
//            this.writeOb = this.writeOb ?? Observable.Create<WriteRequest>(ob =>
//            {
//                var handler = new TypedEventHandler<GattLocalCharacteristic, GattWriteRequestedEventArgs>(async (sender, args) =>
//                {
//                    var request = await args.GetRequestAsync();
//                    var bytes = request.Value.ToArray();
//                    var respond = request.Option == GattWriteOption.WriteWithResponse;
//                    ob.OnNext(new WriteRequest(null, bytes, (int)request.Offset, respond));

//                    if (respond)
//                    {
//                        request.Respond();
//                    }
//                });
//                var sub = this.nativeReady.Subscribe(dev => this.native.WriteRequested += handler);
//                return () =>
//                {
//                    sub.Dispose();
//                    if (this.native != null)
//                        this.native.WriteRequested -= handler;
//                };
//            })
//            .Publish()
//            .RefCount();

//            return this.writeOb;
//        }


//        IObservable<ReadRequest> readOb;
//        public override IObservable<ReadRequest> WhenReadReceived()
//        {
//            this.readOb = this.readOb ?? Observable.Create<ReadRequest>(ob =>
//            {
//                var handler = new TypedEventHandler<GattLocalCharacteristic, GattReadRequestedEventArgs>(async (sender, args) =>
//                {
//                    var request = await args.GetRequestAsync();
//                    var dev = this.FindDevice(args.Session);
//                    var read = new ReadRequest(dev, (int)request.Length);
//                    ob.OnNext(read);

//                    if (read.Status == GattState.Success)
//                        request.RespondWithValue(read.Value.AsBuffer());
//                    else
//                        request.RespondWithProtocolError((byte) read.Status);
//                });
//                var sub = this.nativeReady.Subscribe(dev => this.native.ReadRequested += handler);
//                return () =>
//                {
//                    sub.Dispose();
//                    if (this.native != null)
//                        this.native.ReadRequested -= handler;
//                };
//            })
//            .Publish()
//            .RefCount();

//            return this.readOb;
//        }


//        public async Task Init(GattLocalService gatt)
//        {
//            var ch = await gatt.CreateCharacteristicAsync(
//                this.Uuid,
//                new GattLocalCharacteristicParameters
//                {
//                    CharacteristicProperties = this.ToNative(this.Properties),

//                    ReadProtectionLevel = this.Permissions.HasFlag(GattPermissions.ReadEncrypted)
//                        ? GattProtectionLevel.EncryptionAndAuthenticationRequired
//                        : GattProtectionLevel.Plain,

//                    WriteProtectionLevel =this.Permissions.HasFlag(GattPermissions.WriteEncrypted)
//                        ? GattProtectionLevel.EncryptionAndAuthenticationRequired
//                        : GattProtectionLevel.Plain,
//                }
//            );
//            foreach (var descriptor in this.Descriptors.OfType<IUwpGattDescriptor>())
//            {
//                await descriptor.Init(ch.Characteristic);
//            }
//            this.native = ch.Characteristic;
//            this.nativeReady.OnNext(ch.Characteristic);
//        }


//        protected GattCharacteristicProperties ToNative(CharacteristicProperties props)
//        {
//            var value = props
//                .ToString()
//                .Replace(CharacteristicProperties.WriteNoResponse.ToString(), GattCharacteristicProperties.WriteWithoutResponse.ToString())
//                .Replace(CharacteristicProperties.NotifyEncryptionRequired.ToString(), String.Empty)
//                .Replace(CharacteristicProperties.IndicateEncryptionRequired.ToString(), String.Empty);

//            return (GattCharacteristicProperties)Enum.Parse(typeof(GattCharacteristicProperties), value);
//        }


//        protected virtual IPeripheral FindDevice(GattSession session)
//        {
//            foreach (var device in this.SubscribedDevices)
//            {
//                if (device.Uuid.ToString().Equals(session.DeviceId.Id))
//                    return device;
//            }
//            return null;
//        }


//        protected virtual IPeripheral AddConnectedDevice(GattSession session)
//        {
//            var dev = new UwpPeripheral(session);
//            this.connectedDevices.Add(dev);
//            return dev;
//        }